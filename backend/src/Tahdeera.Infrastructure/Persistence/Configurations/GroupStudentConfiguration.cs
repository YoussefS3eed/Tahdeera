using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class GroupStudentConfiguration : IEntityTypeConfiguration<GroupStudent>
{
    public void Configure(EntityTypeBuilder<GroupStudent> builder)
    {
        builder.ToTable("group_students", t =>
        {
            t.HasCheckConstraint("CK_group_students_status", "status IN ('Active', 'Withdrawn', 'Suspended')");
            t.HasCheckConstraint("CK_group_students_left_at", "left_at IS NULL OR left_at >= joined_at");
            t.HasCheckConstraint("CK_group_students_active_left_at", "(status = 'Active' AND left_at IS NULL) OR status <> 'Active'");
        });

        builder.HasKey(gs => gs.Id);

        builder.Property(gs => gs.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(gs => gs.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(GroupStudentStatus.Active)
            .HasSentinel((GroupStudentStatus)0)
            .IsRequired();

        builder.Property(gs => gs.JoinedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(gs => gs.LeftAt);

        builder.Property(gs => gs.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(gs => new { gs.GroupId, gs.StudentId })
            .IsUnique();

        builder.HasIndex(gs => gs.StudentId)
            .HasDatabaseName("ix_group_students_student");

        builder.HasIndex(gs => new { gs.GroupId, gs.Status })
            .HasDatabaseName("ix_group_students_group_status");

        builder.HasOne(gs => gs.Group)
            .WithMany(g => g.EnrolledStudents)
            .HasForeignKey(gs => gs.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(gs => gs.Student)
            .WithMany(s => s.GroupEnrollments)
            .HasForeignKey(gs => gs.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(gs => gs.UpdatedByUser)
            .WithMany()
            .HasForeignKey(gs => gs.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(gs => gs.StatusHistory)
            .WithOne(h => h.GroupStudent)
            .HasForeignKey(h => h.GroupStudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
