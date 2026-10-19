using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class GroupAttendanceDelegationConfiguration : IEntityTypeConfiguration<GroupAttendanceDelegation>
{
    public void Configure(EntityTypeBuilder<GroupAttendanceDelegation> builder)
    {
        builder.ToTable("group_attendance_delegations", t =>
        {
            t.HasCheckConstraint("CK_group_attendance_delegations_revoked_at", "revoked_at IS NULL OR revoked_at >= granted_at");
        });

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.GrantedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(d => d.RevokedAt);

        builder.Property(d => d.RevocationReason);

        builder.HasIndex(d => d.GroupId)
            .IsUnique()
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("ux_one_active_attendance_delegate_per_group");

        builder.HasIndex(d => d.AssistantUserId)
            .HasDatabaseName("ix_group_delegation_assistant");

        builder.HasOne(d => d.Group)
            .WithMany(g => g.Delegations)
            .HasForeignKey(d => d.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.AssistantUser)
            .WithMany()
            .HasForeignKey(d => d.AssistantUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.GrantedByTeacherUser)
            .WithMany()
            .HasForeignKey(d => d.GrantedByTeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.RevokedByUser)
            .WithMany()
            .HasForeignKey(d => d.RevokedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
