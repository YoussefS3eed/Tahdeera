using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class WorkspaceStudentConfiguration : IEntityTypeConfiguration<WorkspaceStudent>
{
    public void Configure(EntityTypeBuilder<WorkspaceStudent> builder)
    {
        builder.ToTable("workspace_students", t =>
        {
            t.HasCheckConstraint("ck_workspace_students_status", "status IN ('Active', 'Inactive', 'Archived', 'Blocked')");
        });

        builder.HasKey(ws => ws.Id);

        builder.Property(ws => ws.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(ws => ws.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(WorkspaceStudentStatus.Active)
            .HasSentinel((WorkspaceStudentStatus)0)
            .IsRequired();

        builder.Property(ws => ws.StudentPhone)
            .HasMaxLength(32);

        builder.Property(ws => ws.GuardianName)
            .HasMaxLength(200);

        builder.Property(ws => ws.GuardianPhone)
            .HasMaxLength(32);

        builder.Property(ws => ws.WhatsappOptIn)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(ws => ws.SeatNumber)
            .HasMaxLength(30);

        builder.Property(ws => ws.LocalNotes)
            .HasColumnType("text");

        builder.Property(ws => ws.EnrolledAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(ws => ws.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(ws => new { ws.WorkspaceId, ws.StudentId })
            .IsUnique();

        builder.HasIndex(ws => ws.StudentId)
            .HasDatabaseName("ix_workspace_students_student");

        builder.HasIndex(ws => ws.GuardianPhone)
            .HasDatabaseName("ix_workspace_students_guardian_phone");

        builder.HasOne(ws => ws.Workspace)
            .WithMany()
            .HasForeignKey(ws => ws.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ws => ws.Student)
            .WithMany(s => s.WorkspaceEnrollments)
            .HasForeignKey(ws => ws.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ws => ws.CreatedByUser)
            .WithMany()
            .HasForeignKey(ws => ws.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
