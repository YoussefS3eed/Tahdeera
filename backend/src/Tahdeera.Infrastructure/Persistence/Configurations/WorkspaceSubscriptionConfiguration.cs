using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class WorkspaceSubscriptionConfiguration : IEntityTypeConfiguration<WorkspaceSubscription>
{
    public void Configure(EntityTypeBuilder<WorkspaceSubscription> builder)
    {
        builder.ToTable("workspace_subscriptions", t =>
        {
            t.HasCheckConstraint("ck_workspace_subscriptions_ends_after_starts", "ends_at > starts_at");
            t.HasCheckConstraint("ck_workspace_subscriptions_agreed_price", "agreed_price >= 0");
            t.HasCheckConstraint("ck_workspace_subscriptions_status", "status IN ('Active', 'Expired', 'Cancelled')");
            t.HasCheckConstraint("ck_workspace_subscriptions_suspension_reason", "(manual_suspended_at IS NULL) OR (suspension_reason IS NOT NULL AND length(trim(suspension_reason)) > 0)");
            t.HasCheckConstraint("ck_workspace_subscriptions_max_students_override", "max_students_override IS NULL OR max_students_override > 0");
            t.HasCheckConstraint("ck_workspace_subscriptions_max_staff_override", "max_staff_override IS NULL OR max_staff_override > 0");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StartsAt)
            .IsRequired();

        builder.Property(s => s.EndsAt)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasMaxLength(20)
            .HasDefaultValue("Active")
            .IsRequired();

        builder.Property(s => s.SuspensionReason)
            .HasColumnType("text");

        builder.Property(s => s.AgreedPrice)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(s => s.FeatureOverrides)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}")
            .IsRequired();

        builder.HasIndex(s => new { s.WorkspaceId, s.EndsAt })
            .HasDatabaseName("ix_workspace_subscriptions_workspace_end");

        builder.HasOne(s => s.Workspace)
            .WithMany(w => w.Subscriptions)
            .HasForeignKey(s => s.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Plan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.ManualSuspendedByUser)
            .WithMany()
            .HasForeignKey(s => s.ManualSuspendedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
