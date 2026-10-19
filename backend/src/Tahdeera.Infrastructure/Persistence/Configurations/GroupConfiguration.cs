using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups", t =>
        {
            t.HasCheckConstraint("CK_groups_late_grace_minutes", "late_grace_minutes >= 0 AND late_grace_minutes <= 1440");
            t.HasCheckConstraint("CK_groups_default_monthly_fee", "default_monthly_fee >= 0");
            t.HasCheckConstraint("CK_groups_consecutive_absence_threshold", "consecutive_absence_threshold IS NULL OR (consecutive_absence_threshold > 0 AND consecutive_absence_threshold <= 50)");
            t.HasCheckConstraint("CK_groups_status", "status IN ('Active', 'Paused', 'Archived')");
            t.HasCheckConstraint("CK_groups_absence_action", "absence_action IN ('NotifyOnly', 'MarkSuspended', 'Archive')");
        });

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(g => g.Name)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(g => g.RoomName)
            .HasMaxLength(100);

        builder.Property(g => g.ScheduleSummary)
            .HasMaxLength(240);

        builder.Property(g => g.LateGraceMinutes)
            .HasDefaultValue(20)
            .IsRequired();

        builder.Property(g => g.DefaultMonthlyFee)
            .HasPrecision(12, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(g => g.ConsecutiveAbsenceThreshold);

        builder.Property(g => g.AbsenceAction)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AbsenceAction.NotifyOnly)
            .HasSentinel((AbsenceAction)0)
            .IsRequired();

        builder.Property(g => g.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(GroupStatus.Active)
            .HasSentinel((GroupStatus)0)
            .IsRequired();

        builder.Property(g => g.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(g => g.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(g => new { g.OwnerWorkspaceId, g.AcademicYearId, g.Name })
            .IsUnique();

        builder.HasIndex(g => new { g.OwnerWorkspaceId, g.Status })
            .HasDatabaseName("ix_groups_owner_status");

        builder.HasIndex(g => g.ResponsibleTeacherUserId)
            .HasDatabaseName("ix_groups_responsible_teacher");

        builder.HasOne(g => g.OwnerWorkspace)
            .WithMany(w => w.Groups)
            .HasForeignKey(g => g.OwnerWorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.AcademicYear)
            .WithMany()
            .HasForeignKey(g => g.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Grade)
            .WithMany()
            .HasForeignKey(g => g.GradeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Subject)
            .WithMany()
            .HasForeignKey(g => g.SubjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(g => g.ResponsibleTeacherUser)
            .WithMany()
            .HasForeignKey(g => g.ResponsibleTeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.CreatedByUser)
            .WithMany()
            .HasForeignKey(g => g.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
