using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions", t =>
        {
            t.HasCheckConstraint("CK_sessions_ends_at", "ends_at > starts_at");
            t.HasCheckConstraint("CK_sessions_late_grace_minutes", "late_grace_minutes >= 0 AND late_grace_minutes <= 1440");
            t.HasCheckConstraint("CK_sessions_status", "status IN ('Scheduled', 'Completed', 'Cancelled')");
            t.HasCheckConstraint("CK_sessions_cancellation", "(status = 'Cancelled' AND cancellation_reason IS NOT NULL) OR (status <> 'Cancelled' AND cancellation_reason IS NULL)");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.StartsAt)
            .IsRequired();

        builder.Property(s => s.EndsAt)
            .IsRequired();

        builder.Property(s => s.LateGraceMinutes)
            .HasDefaultValue(20)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(SessionStatus.Scheduled)
            .HasSentinel((SessionStatus)0)
            .IsRequired();

        builder.Property(s => s.CancellationReason);

        builder.Property(s => s.CancelledAt);

        builder.Property(s => s.AutoAbsenceProcessedAt);

        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(s => new { s.GroupId, s.StartsAt })
            .HasDatabaseName("ix_sessions_group_start");

        builder.HasIndex(s => s.EndsAt)
            .HasFilter("status <> 'Cancelled'")
            .HasDatabaseName("ix_sessions_due_for_attendance");

        builder.HasOne(s => s.Group)
            .WithMany(g => g.Sessions)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.CancelledByUser)
            .WithMany()
            .HasForeignKey(s => s.CancelledByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(s => s.AttendanceRecords)
            .WithOne(ar => ar.Session)
            .HasForeignKey(ar => ar.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
