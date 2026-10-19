using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records", t =>
        {
            t.HasCheckConstraint("CK_attendance_records_status", "status IN ('Present', 'Absent', 'Late', 'Excused')");
            t.HasCheckConstraint("CK_attendance_records_source", "source IN ('System', 'User')");
            t.HasCheckConstraint("CK_attendance_records_source_user", "(source = 'System' AND recorded_by_user_id IS NULL) OR (source = 'User' AND recorded_by_user_id IS NOT NULL)");
            t.HasCheckConstraint("CK_attendance_records_check_in", "(status IN ('Present', 'Late') AND check_in_at IS NOT NULL) OR status IN ('Absent', 'Excused')");
            t.HasCheckConstraint("CK_attendance_records_retroactive_reason", "retroactive_reason IS NULL OR length(trim(retroactive_reason)) > 0");
            t.HasCheckConstraint("CK_attendance_records_void", "voided_at IS NULL OR void_reason IS NOT NULL");
        });

        builder.HasKey(ar => ar.Id);

        builder.Property(ar => ar.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(ar => ar.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ar => ar.Source)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ar => ar.AutoEvaluationLocked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(ar => ar.RecordedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(ar => ar.CheckInAt);

        builder.Property(ar => ar.LastModifiedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(ar => ar.RetroactiveReason);

        builder.Property(ar => ar.SystemGeneratedAt);

        builder.Property(ar => ar.VoidedAt);

        builder.Property(ar => ar.VoidReason);

        builder.Property(ar => ar.IsGuestMakeup)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(ar => new { ar.SessionId, ar.StudentId })
            .IsUnique();

        builder.HasIndex(ar => new { ar.OwnerWorkspaceId, ar.SessionId })
            .HasFilter("voided_at IS NULL")
            .HasDatabaseName("ix_attendance_owner_session_active");

        builder.HasIndex(ar => new { ar.StudentId, ar.RecordedAt })
            .IsDescending(false, true)
            .HasFilter("voided_at IS NULL")
            .HasDatabaseName("ix_attendance_student_recorded_active");

        builder.HasIndex(ar => ar.SessionId)
            .HasFilter("auto_evaluation_locked = false AND voided_at IS NULL")
            .HasDatabaseName("ix_attendance_auto_reevaluate");

        builder.HasIndex(ar => new { ar.StudentId, ar.IsGuestMakeup })
            .HasFilter("is_guest_makeup = true")
            .HasDatabaseName("ix_attendance_guest_makeup");

        builder.HasOne(ar => ar.Session)
            .WithMany(s => s.AttendanceRecords)
            .HasForeignKey(ar => ar.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ar => ar.Student)
            .WithMany(s => s.AttendanceRecords)
            .HasForeignKey(ar => ar.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ar => ar.OwnerWorkspace)
            .WithMany(w => w.AttendanceRecords)
            .HasForeignKey(ar => ar.OwnerWorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ar => ar.HomeGroup)
            .WithMany()
            .HasForeignKey(ar => ar.HomeGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ar => ar.RecordedByUser)
            .WithMany()
            .HasForeignKey(ar => ar.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ar => ar.LastModifiedByUser)
            .WithMany()
            .HasForeignKey(ar => ar.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(ar => ar.HistoryEntries)
            .WithOne(h => h.AttendanceRecord)
            .HasForeignKey(h => h.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(ar => ar.OutboxMessages)
            .WithOne(m => m.AttendanceRecord)
            .HasForeignKey(m => m.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
