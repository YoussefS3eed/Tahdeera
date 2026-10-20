using Tahdeera.Domain.Entities;

namespace Tahdeera.Application.Common.Interfaces;

public sealed record OutboxPublishResult(
    bool Success,
    string? BrokerMessageId = null,
    string? ErrorCode = null,
    string? ErrorDetails = null)
{
    public static OutboxPublishResult Succeeded(string brokerMessageId)
        => new(true, BrokerMessageId: brokerMessageId);

    public static OutboxPublishResult Failed(string errorCode, string errorDetails)
        => new(false, ErrorCode: errorCode, ErrorDetails: errorDetails);
}

public interface IMessageOutboxPublisher
{
    Task<OutboxPublishResult> PublishAsync(MessageOutbox message, CancellationToken cancellationToken = default);
}
