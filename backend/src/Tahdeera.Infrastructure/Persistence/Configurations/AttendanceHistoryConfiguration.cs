using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class AttendanceHistoryConfiguration : IEntityTypeConfiguration<AttendanceHistory>
{
    public void Configure(EntityTypeBuilder<AttendanceHistory> builder)
    {
        builder.ToTable("attendance_history", t =>
        {
            t.HasCheckConstraint("CK_attendance_history_old_status", "old_status IS NULL OR old_status IN ('Present', 'Absent', 'Late', 'Excused')");
            t.HasCheckConstraint("CK_attendance_history_new_status", "new_status IN ('Present', 'Absent', 'Late', 'Excused')");
            t.HasCheckConstraint("CK_attendance_history_old_source", "old_source IS NULL OR old_source IN ('System', 'User')");
            t.HasCheckConstraint("CK_attendance_history_new_source", "new_source IN ('System', 'User')");
            t.HasCheckConstraint("CK_attendance_history_change_kind", "change_kind IN ('InitialSystemAbsence', 'InitialUserEntry', 'ManualCorrection', 'SystemReevaluation', 'VoidedOnCancellation', 'RestoredFromCancellation', 'Other')");
        });

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .UseIdentityByDefaultColumn();

        builder.Property(h => h.OldStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(h => h.OldSource)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.NewSource)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(h => h.ChangedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(h => h.Reason);

        builder.Property(h => h.ChangeKind)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(h => h.Metadata)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.HasIndex(h => new { h.AttendanceRecordId, h.ChangedAt })
            .HasDatabaseName("ix_attendance_history_record_time");

        builder.HasOne(h => h.AttendanceRecord)
            .WithMany(ar => ar.HistoryEntries)
            .HasForeignKey(h => h.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
