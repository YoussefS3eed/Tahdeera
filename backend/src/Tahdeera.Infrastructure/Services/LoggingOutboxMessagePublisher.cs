using Microsoft.Extensions.Logging;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Services;

public sealed class LoggingOutboxMessagePublisher : IMessageOutboxPublisher
{
    private readonly ILogger<LoggingOutboxMessagePublisher> _logger;

    public LoggingOutboxMessagePublisher(ILogger<LoggingOutboxMessagePublisher> logger)
    {
        _logger = logger;
    }

    public Task<OutboxPublishResult> PublishAsync(MessageOutbox message, CancellationToken cancellationToken = default)
    {
        var brokerMessageId = $"broker_{Guid.NewGuid():N}";
        _logger.LogInformation(
            "Outbox message {OutboxId} of type {MessageType} for recipient {Recipient} dispatched to broker with ID {BrokerMessageId}",
            message.Id,
            message.MessageType,
            message.RecipientPhone,
            brokerMessageId);

        return Task.FromResult(OutboxPublishResult.Succeeded(brokerMessageId));
    }
}
