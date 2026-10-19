using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class GroupWorkspaceAccessConfiguration : IEntityTypeConfiguration<GroupWorkspaceAccess>
{
    public void Configure(EntityTypeBuilder<GroupWorkspaceAccess> builder)
    {
        builder.ToTable("group_workspace_access", t =>
        {
            t.HasCheckConstraint("CK_group_workspace_access_revoked_at", "revoked_at IS NULL OR revoked_at >= granted_at");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.CanViewRoster)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(a => a.CanRegisterAttendance)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(a => a.CanEditAttendance)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(a => a.GrantedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(a => a.RevokedAt);

        builder.Property(a => a.Notes);

        builder.HasIndex(a => new { a.GroupId, a.WorkspaceId })
            .IsUnique();

        builder.HasIndex(a => a.WorkspaceId)
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("ix_group_workspace_access_workspace");

        builder.HasOne(a => a.Group)
            .WithMany(g => g.WorkspaceAccesses)
            .HasForeignKey(a => a.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Workspace)
            .WithMany(w => w.WorkspaceAccesses)
            .HasForeignKey(a => a.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.GrantedByUser)
            .WithMany()
            .HasForeignKey(a => a.GrantedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.RevokedByUser)
            .WithMany()
            .HasForeignKey(a => a.RevokedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
