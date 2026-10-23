using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class GroupStudentStatusHistoryConfiguration : IEntityTypeConfiguration<GroupStudentStatusHistory>
{
    public void Configure(EntityTypeBuilder<GroupStudentStatusHistory> builder)
    {
        builder.ToTable("group_student_status_history", t =>
        {
            t.HasCheckConstraint("CK_group_student_status_history_old_status", "old_status IS NULL OR old_status IN ('Active', 'Withdrawn', 'Suspended')");
            t.HasCheckConstraint("CK_group_student_status_history_new_status", "new_status IN ('Active', 'Withdrawn', 'Suspended')");
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

        builder.Property(h => h.EffectiveAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(h => h.Reason);

        builder.Property(h => h.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(h => new { h.GroupStudentId, h.EffectiveAt })
            .HasDatabaseName("ix_group_student_history_membership_time");

        builder.HasOne(h => h.GroupStudent)
            .WithMany(gs => gs.StatusHistory)
            .HasForeignKey(h => h.GroupStudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
