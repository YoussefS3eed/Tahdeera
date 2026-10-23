using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Domain.Entities;

public class MessageOutbox : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? AttendanceRecordId { get; set; }
    public Guid? ExamSubmissionId { get; set; }
    public string RecipientPhone { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "ar";
    public string MessageType { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public string PayloadJson
    {
        get => Payload;
        set => Payload = value;
    }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? BrokerMessageId { get; set; }
    public string? ProviderMessageId { get; set; }
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public string? FailureCategory { get; set; }
    public short Attempts { get; set; } = 0;
    public int AttemptCount => Attempts;
    public short MaxAttempts { get; set; } = 5;
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorDetails { get; set; }
    public string? ErrorMessage
    {
        get => ErrorDetails;
        set => ErrorDetails = value;
    }

    // Milestone B4 Hybrid Fields
    public string SenderMode { get; set; } = "PlatformManaged";
    public Guid? SenderConnectionId { get; set; }
    public short LeaseRecoveryAttempts { get; set; } = 0;
    public decimal? CostEgp { get; set; }

    // Navigation properties
    public virtual Workspace? Workspace { get; set; }
    public virtual Student? Student { get; set; }
    public virtual Session? Session { get; set; }
    public virtual AttendanceRecord? AttendanceRecord { get; set; }
    public virtual WhatsAppSenderConnection? SenderConnection { get; set; }

    public MessageOutbox() { } // EF Core

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
            TemplateName = messageType.Trim(),
            Payload = payload,
            IdempotencyKey = idempotencyKey.Trim(),
            LanguageCode = languageCode,
            SenderMode = "PlatformManaged",
            Status = OutboxMessageStatus.Pending,
            Attempts = 0,
            MaxAttempts = 5,
            LeaseRecoveryAttempts = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static MessageOutbox Create(
        Guid workspaceId,
        string recipientPhone,
        string messageType,
        string templateName,
        string languageCode,
        string payloadJson,
        string idempotencyKey,
        string senderMode = "PlatformManaged",
        Guid? senderConnectionId = null,
        Guid? studentId = null,
        Guid? sessionId = null,
        Guid? attendanceRecordId = null,
        short maxAttempts = 5)
    {
        var now = DateTimeOffset.UtcNow;
        return new MessageOutbox
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            SessionId = sessionId,
            AttendanceRecordId = attendanceRecordId,
            RecipientPhone = recipientPhone,
            MessageType = messageType,
            TemplateName = templateName,
            LanguageCode = languageCode,
            Payload = payloadJson,
            IdempotencyKey = idempotencyKey,
            SenderMode = senderMode,
            SenderConnectionId = senderConnectionId,
            Status = OutboxMessageStatus.Pending,
            Attempts = 0,
            MaxAttempts = maxAttempts,
            LeaseRecoveryAttempts = 0,
            NextAttemptAt = null,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void TransitionToProcessing(TimeSpan leaseDuration)
    {
        Status = OutboxMessageStatus.Processing;
        Attempts++;
        LastAttemptAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
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

    public void TransitionToSent(string providerMessageId, decimal costEgp)
    {
        // Guard against out-of-order webhook delivery race condition
        if (Status != OutboxMessageStatus.Delivered)
        {
            Status = OutboxMessageStatus.Sent;
        }

        ProviderMessageId = providerMessageId;
        SentAt = DateTimeOffset.UtcNow;
        CostEgp = costEgp;
        NextAttemptAt = null;
        ErrorCode = null;
        ErrorDetails = null;
        UpdatedAt = DateTimeOffset.UtcNow;
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

    public bool TransitionToDelivered(DateTimeOffset deliveredAt)
    {
        if (Status == OutboxMessageStatus.Delivered)
        {
            return false;
        }

        if (SentAt == null)
        {
            SentAt = deliveredAt;
        }

        Status = OutboxMessageStatus.Delivered;
        DeliveredAt = deliveredAt;
        NextAttemptAt = null;
        ErrorCode = null;
        ErrorDetails = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public void RecordReadReceipt(DateTimeOffset readAt)
    {
        try
        {
            var node = string.IsNullOrWhiteSpace(Payload)
                ? new System.Text.Json.Nodes.JsonObject()
                : System.Text.Json.Nodes.JsonNode.Parse(Payload) ?? new System.Text.Json.Nodes.JsonObject();

            if (node is System.Text.Json.Nodes.JsonObject obj)
            {
                obj["meta_read_at"] = readAt.ToString("O");
                Payload = obj.ToJsonString();
            }
            else
            {
                var fallback = new System.Text.Json.Nodes.JsonObject
                {
                    ["payload"] = node,
                    ["meta_read_at"] = readAt.ToString("O")
                };
                Payload = fallback.ToJsonString();
            }
            UpdatedAt = DateTimeOffset.UtcNow;
        }
        catch
        {
            var fallback = new System.Text.Json.Nodes.JsonObject
            {
                ["raw_payload"] = Payload,
                ["meta_read_at"] = readAt.ToString("O")
            };
            Payload = fallback.ToJsonString();
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void MarkDelivered(DateTimeOffset deliveredAt)
    {
        TransitionToDelivered(deliveredAt);
    }

    public void TransitionToFailedTransient(string errorCode, string errorMessage, TimeSpan retryDelay)
    {
        Status = OutboxMessageStatus.FailedTransient;
        FailureCategory = "Transient";
        ErrorCode = errorCode;
        ErrorDetails = errorMessage;
        NextAttemptAt = DateTimeOffset.UtcNow.Add(retryDelay);
        UpdatedAt = DateTimeOffset.UtcNow;
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

    public void TransitionToFailedPermanent(string errorCode, string errorMessage)
    {
        Status = OutboxMessageStatus.FailedPermanent;
        FailureCategory = "Permanent";
        ErrorCode = errorCode;
        ErrorDetails = errorMessage;
        NextAttemptAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
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

    public void RecoverLease()
    {
        LeaseRecoveryAttempts++;
        if (LeaseRecoveryAttempts >= 3)
        {
            TransitionToFailedPermanent(
                "STALE_LEASE_EXCEEDED",
                "Message processing exceeded maximum 3 lease recovery cycles without completion.");
        }
        else
        {
            Status = OutboxMessageStatus.Pending;
            NextAttemptAt = DateTimeOffset.UtcNow;
        }
        UpdatedAt = DateTimeOffset.UtcNow;
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


    public void RecordAttempt(WhatsAppSenderMode mode, string? phoneNumberId)
    {
        SenderMode = mode.ToString();
        Attempts++;
        LastAttemptAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ScheduleRetryAt(DateTimeOffset nextAttemptAt, string? errorCode, string? errorMessage)
    {
        Attempts++;
        Status = OutboxMessageStatus.FailedTransient;
        FailureCategory = "Transient";
        ErrorCode = errorCode;
        ErrorDetails = errorMessage;
        NextAttemptAt = nextAttemptAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void TransitionToSent(string providerMessageId, decimal costUsd, decimal costEgp, DateTimeOffset sentAt)
    {
        if (Status != OutboxMessageStatus.Delivered)
        {
            Status = OutboxMessageStatus.Sent;
        }

        ProviderMessageId = providerMessageId;
        SentAt = sentAt;
        CostEgp = costEgp;
        NextAttemptAt = null;
        ErrorCode = null;
        ErrorDetails = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    public void ResetForManualRetry(string refreshedPhone)
    {
        if (Status == OutboxMessageStatus.Sent || Status == OutboxMessageStatus.Delivered)
            throw new DomainValidationException("MESSAGE_ALREADY_SENT", "The message has already been processed and delivered.");

        if (Status == OutboxMessageStatus.PublishedToBroker || Status == OutboxMessageStatus.Processing)
            throw new DomainValidationException("MESSAGE_IN_FLIGHT", "The message is currently in-flight in the broker and cannot be retried.");

        if (Status != OutboxMessageStatus.FailedPermanent && Status != OutboxMessageStatus.FailedTransient)
            throw new DomainValidationException("INVALID_STATUS_FOR_RETRY", $"Cannot retry message from status '{Status}'. Only FailedPermanent or FailedTransient messages can be retried.");

        RecipientPhone = refreshedPhone.Trim();
        Status = OutboxMessageStatus.Pending;
        FailureCategory = null;
        Attempts = 0;
        LeaseRecoveryAttempts = 0;
        ErrorCode = null;
        ErrorDetails = null;
        NextAttemptAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
