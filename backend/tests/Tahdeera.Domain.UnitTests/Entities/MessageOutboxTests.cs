using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.UnitTests.Entities;

public class MessageOutboxTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _recordId = Guid.NewGuid();

    private MessageOutbox CreateSampleOutbox()
    {
        return MessageOutbox.CreateForAttendance(
            _workspaceId,
            _studentId,
            _recordId,
            "+201012345678",
            "AttendanceCheckIn",
            "{\"studentName\":\"Omar\"}",
            $"att_{_recordId}_Present");
    }

    [Fact]
    public void ClaimForProcessing_Increments_Attempts_And_Sets_LastAttemptAt()
    {
        var outbox = CreateSampleOutbox();
        var claimTime = DateTimeOffset.UtcNow;

        outbox.ClaimForProcessing(claimTime);

        outbox.Status.Should().Be(OutboxMessageStatus.Processing);
        outbox.Attempts.Should().Be(1);
        outbox.LastAttemptAt.Should().Be(claimTime);
    }

    [Fact]
    public void ScheduleRetry_Calculates_Exponential_Backoff_Correctly()
    {
        var outbox = CreateSampleOutbox();
        var now = DateTimeOffset.UtcNow;

        // Attempt 1: backoff is 5 * 2^0 = 5 seconds
        outbox.ClaimForProcessing(now);
        outbox.ScheduleRetry(now, "TIMEOUT", "Gateway timeout", jitterSeconds: 2);

        outbox.Status.Should().Be(OutboxMessageStatus.FailedTransient);
        outbox.FailureCategory.Should().Be("Transient");
        outbox.NextAttemptAt.Should().Be(now.AddSeconds(5 + 2));

        // Attempt 2: backoff is 5 * 2^1 = 10 seconds
        var nextTime = now.AddSeconds(10);
        outbox.ClaimForProcessing(nextTime);
        outbox.ScheduleRetry(nextTime, "TIMEOUT", "Gateway timeout 2", jitterSeconds: 0);

        outbox.Status.Should().Be(OutboxMessageStatus.FailedTransient);
        outbox.NextAttemptAt.Should().Be(nextTime.AddSeconds(10));
    }

    [Fact]
    public void ScheduleRetry_Transitions_To_FailedPermanent_When_Attempts_Reach_MaxAttempts()
    {
        var outbox = CreateSampleOutbox();
        outbox.MaxAttempts = 2;
        var now = DateTimeOffset.UtcNow;

        // Attempt 1
        outbox.ClaimForProcessing(now);
        outbox.ScheduleRetry(now, "ERR", "Fail 1");
        outbox.Status.Should().Be(OutboxMessageStatus.FailedTransient);

        // Attempt 2 (reaches MaxAttempts)
        outbox.ClaimForProcessing(now.AddSeconds(10));
        outbox.ScheduleRetry(now.AddSeconds(10), "ERR", "Fail 2");

        outbox.Status.Should().Be(OutboxMessageStatus.FailedPermanent);
        outbox.FailureCategory.Should().Be("Permanent");
        outbox.NextAttemptAt.Should().BeNull();
        outbox.ErrorDetails.Should().Contain("Retries exhausted (2/2)");
    }

    [Fact]
    public void MarkSent_Sets_ProviderMessageId_And_SentAt()
    {
        var outbox = CreateSampleOutbox();
        outbox.ClaimForProcessing(DateTimeOffset.UtcNow);

        var sentTime = DateTimeOffset.UtcNow;
        outbox.MarkSent("wamid.HBgLM...", sentTime);

        outbox.Status.Should().Be(OutboxMessageStatus.Sent);
        outbox.ProviderMessageId.Should().Be("wamid.HBgLM...");
        outbox.SentAt.Should().Be(sentTime);
        outbox.NextAttemptAt.Should().BeNull();
    }

    [Fact]
    public void MarkDelivered_Sets_DeliveredAt()
    {
        var outbox = CreateSampleOutbox();
        outbox.ClaimForProcessing(DateTimeOffset.UtcNow);
        var sentTime = DateTimeOffset.UtcNow;
        outbox.MarkSent("wamid.123", sentTime);

        var deliveredTime = sentTime.AddSeconds(2);
        outbox.MarkDelivered(deliveredTime);

        outbox.Status.Should().Be(OutboxMessageStatus.Delivered);
        outbox.DeliveredAt.Should().Be(deliveredTime);
    }

    [Fact]
    public void Cancel_Transitions_To_Cancelled_And_Clears_NextAttemptAt()
    {
        var outbox = CreateSampleOutbox();
        outbox.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(5);

        outbox.Cancel("Session cancelled");

        outbox.Status.Should().Be(OutboxMessageStatus.Cancelled);
        outbox.NextAttemptAt.Should().BeNull();
        outbox.ErrorDetails.Should().Be("Cancelled: Session cancelled");
    }

    [Fact]
    public void Cancel_Throws_When_Message_Already_Sent_Or_Delivered()
    {
        var outbox = CreateSampleOutbox();
        outbox.ClaimForProcessing(DateTimeOffset.UtcNow);
        outbox.MarkSent("wamid.123", DateTimeOffset.UtcNow);

        var act = () => outbox.Cancel("Too late");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot cancel message in terminal status 'Sent'.");
    }

    [Fact]
    public void Terminal_States_Reject_Requeue_Or_Processing()
    {
        var outbox = CreateSampleOutbox();
        outbox.ClaimForProcessing(DateTimeOffset.UtcNow);
        outbox.MarkSent("wamid.123", DateTimeOffset.UtcNow);

        var act = () => outbox.ClaimForProcessing(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot claim outbox message for processing from status 'Sent'.");
    }
}
