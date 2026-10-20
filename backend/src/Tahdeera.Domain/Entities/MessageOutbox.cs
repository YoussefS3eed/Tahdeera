using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.Entities;

public class MessageOutbox
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? AttendanceRecordId { get; set; }
    public Guid? ExamSubmissionId { get; set; }
    public string RecipientPhone { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "ar";
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? BrokerMessageId { get; set; }
    public string? ProviderMessageId { get; set; }
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public string? FailureCategory { get; set; }
    public short Attempts { get; set; } = 0;
    public short MaxAttempts { get; set; } = 5;
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorDetails { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Workspace Workspace { get; set; } = null!;
    public Student? Student { get; set; }
    public AttendanceRecord? AttendanceRecord { get; set; }

    public static MessageOutbox CreateForAttendance(
        Guid workspaceId,
        Guid studentId,
        Guid attendanceRecordId,
        string recipientPhone,
        string messageType,
        string payload,
        string idempotencyKey,
        string languageCode = "ar")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientPhone);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        var now = DateTimeOffset.UtcNow;
        return new MessageOutbox
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            AttendanceRecordId = attendanceRecordId,
            RecipientPhone = recipientPhone.Trim(),
            MessageType = messageType.Trim(),
            Payload = payload,
            IdempotencyKey = idempotencyKey.Trim(),
            LanguageCode = languageCode,
            Status = OutboxMessageStatus.Pending,
            Attempts = 0,
            MaxAttempts = 5,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void ClaimForProcessing(DateTimeOffset claimedAt)
    {
        if (Status != OutboxMessageStatus.Pending &&
            Status != OutboxMessageStatus.PublishedToBroker &&
            Status != OutboxMessageStatus.FailedTransient)
        {
            throw new InvalidOperationException($"Cannot claim outbox message for processing from status '{Status}'.");
        }

        Status = OutboxMessageStatus.Processing;
        Attempts = (short)(Attempts + 1);
        LastAttemptAt = claimedAt;
        UpdatedAt = claimedAt;
    }

    public void MarkPublishedToBroker(string brokerMessageId, DateTimeOffset publishedAt)
    {
        if (Status != OutboxMessageStatus.Pending && Status != OutboxMessageStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot publish message to broker from status '{Status}'.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(brokerMessageId);

        Status = OutboxMessageStatus.PublishedToBroker;
        BrokerMessageId = brokerMessageId.Trim();
        UpdatedAt = publishedAt;
    }

    public void ReleaseStaleLease(DateTimeOffset now, string reason)
    {
        if (Status != OutboxMessageStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot release stale lease for message not in 'Processing' status. Current: '{Status}'.");
        }

        if (Attempts >= MaxAttempts)
        {
            MarkPermanentFailure("LEASE_EXHAUSTED", $"Lease timed out and maximum attempts reached ({Attempts}/{MaxAttempts}): {reason}");
            return;
        }

        Status = OutboxMessageStatus.FailedTransient;
        FailureCategory = "Transient";
        ErrorCode = "LEASE_TIMEOUT";
        ErrorDetails = $"Lease recovered after timeout: {reason}";
        NextAttemptAt = now;
        UpdatedAt = now;
    }

    public void MarkSent(string providerMessageId, DateTimeOffset sentAt)
    {
        if (Status != OutboxMessageStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot mark message sent from status '{Status}'. Must be in 'Processing'.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(providerMessageId);

        Status = OutboxMessageStatus.Sent;
        ProviderMessageId = providerMessageId.Trim();
        SentAt = sentAt;
        NextAttemptAt = null;
        UpdatedAt = sentAt;
    }

    public void MarkDelivered(DateTimeOffset deliveredAt)
    {
        if (Status != OutboxMessageStatus.Sent && Status != OutboxMessageStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot mark message delivered from status '{Status}'.");
        }

        Status = OutboxMessageStatus.Delivered;
        DeliveredAt = deliveredAt;
        NextAttemptAt = null;
        UpdatedAt = deliveredAt;
    }

    public void ScheduleRetry(DateTimeOffset now, string errorCode, string errorDetails, int jitterSeconds = 0)
    {
        if (Status != OutboxMessageStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot schedule retry from status '{Status}'. Must be in 'Processing'.");
        }

        if (Attempts >= MaxAttempts)
        {
            MarkPermanentFailure(errorCode, $"Retries exhausted ({Attempts}/{MaxAttempts}): {errorDetails}");
            return;
        }

        int delay = Math.Min(300, 5 * (int)Math.Pow(2, Math.Max(0, Attempts - 1)) + Math.Max(0, jitterSeconds));

        Status = OutboxMessageStatus.FailedTransient;
        FailureCategory = "Transient";
        NextAttemptAt = now.AddSeconds(delay);
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
        UpdatedAt = now;
    }

    public void MarkPermanentFailure(string errorCode, string errorDetails)
    {
        Status = OutboxMessageStatus.FailedPermanent;
        FailureCategory = "Permanent";
        NextAttemptAt = null;
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status == OutboxMessageStatus.Sent ||
            Status == OutboxMessageStatus.Delivered ||
            Status == OutboxMessageStatus.FailedPermanent)
        {
            throw new InvalidOperationException($"Cannot cancel message in terminal status '{Status}'.");
        }

        Status = OutboxMessageStatus.Cancelled;
        NextAttemptAt = null;
        ErrorDetails = $"Cancelled: {reason}";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
