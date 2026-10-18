using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("workspace_members", t =>
        {
            t.HasCheckConstraint("ck_workspace_members_role", "role IN ('Owner', 'Teacher', 'Assistant', 'Reception', 'Accountant', 'Member')");
            t.HasCheckConstraint("ck_workspace_members_left_after_joined", "left_at IS NULL OR left_at >= joined_at");
            t.HasCheckConstraint("ck_workspace_members_active_status", "(is_active = true AND left_at IS NULL) OR is_active = false");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(m => m.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(m => m.JoinedAt)
            .IsRequired();

        builder.HasIndex(m => new { m.WorkspaceId, m.UserId })
            .IsUnique();

        builder.HasIndex(m => new { m.UserId, m.IsActive })
            .HasDatabaseName("ix_workspace_members_user_active");

        builder.HasIndex(m => new { m.WorkspaceId, m.Role })
            .HasFilter("is_active = true")
            .HasDatabaseName("ix_workspace_members_workspace_role");

        builder.HasOne(m => m.Workspace)
            .WithMany(w => w.Members)
            .HasForeignKey(m => m.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.User)
            .WithMany(u => u.WorkspaceMemberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.CreatedByUser)
            .WithMany()
            .HasForeignKey(m => m.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
