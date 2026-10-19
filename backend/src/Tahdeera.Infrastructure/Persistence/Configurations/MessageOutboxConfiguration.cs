using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class MessageOutboxConfiguration : IEntityTypeConfiguration<MessageOutbox>
{
    public void Configure(EntityTypeBuilder<MessageOutbox> builder)
    {
        builder.ToTable("message_outbox", t =>
        {
            t.HasCheckConstraint("CK_message_outbox_language_code", "language_code IN ('ar', 'en')");
            t.HasCheckConstraint("CK_message_outbox_message_type", "message_type IN ('AttendanceCheckIn', 'AbsenceAlert', 'ExamResult', 'WeeklyParentReport', 'PaymentReceipt')");
            t.HasCheckConstraint("CK_message_outbox_status", "status IN ('Pending', 'PublishedToBroker', 'Processing', 'Sent', 'Delivered', 'FailedTransient', 'FailedPermanent', 'Cancelled')");
            t.HasCheckConstraint("CK_message_outbox_failure_category", "failure_category IS NULL OR failure_category IN ('Transient', 'Permanent')");
            t.HasCheckConstraint("CK_message_outbox_attempts", "attempts >= 0 AND attempts <= 10");
            t.HasCheckConstraint("CK_message_outbox_max_attempts", "max_attempts >= 1 AND max_attempts <= 10");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(m => m.RecipientPhone)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(m => m.LanguageCode)
            .HasMaxLength(10)
            .HasDefaultValue("ar")
            .IsRequired();

        builder.Property(m => m.MessageType)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(m => m.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(m => m.IdempotencyKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.BrokerMessageId)
            .HasMaxLength(200);

        builder.Property(m => m.ProviderMessageId)
            .HasMaxLength(200);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(OutboxMessageStatus.Pending)
            .HasSentinel((OutboxMessageStatus)0)
            .IsRequired();

        builder.Property(m => m.FailureCategory)
            .HasMaxLength(20);

        builder.Property(m => m.Attempts)
            .HasDefaultValue((short)0)
            .IsRequired();

        builder.Property(m => m.MaxAttempts)
            .HasDefaultValue((short)5)
            .IsRequired();

        builder.Property(m => m.NextAttemptAt);

        builder.Property(m => m.LastAttemptAt);

        builder.Property(m => m.SentAt);

        builder.Property(m => m.DeliveredAt);

        builder.Property(m => m.ErrorCode)
            .HasMaxLength(80);

        builder.Property(m => m.ErrorDetails);

        builder.Property(m => m.ExamSubmissionId);

        builder.Property(m => m.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(m => m.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(m => new { m.Status, m.NextAttemptAt })
            .HasFilter("status IN ('Pending', 'FailedTransient')")
            .HasDatabaseName("ix_message_outbox_relay");

        builder.HasIndex(m => m.AttendanceRecordId)
            .HasDatabaseName("ix_message_outbox_attendance");

        builder.HasIndex(m => new { m.WorkspaceId, m.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_message_outbox_workspace_created");

        builder.HasOne(m => m.Workspace)
            .WithMany(w => w.OutboxMessages)
            .HasForeignKey(m => m.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Student)
            .WithMany(s => s.OutboxMessages)
            .HasForeignKey(m => m.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.AttendanceRecord)
            .WithMany(ar => ar.OutboxMessages)
            .HasForeignKey(m => m.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
