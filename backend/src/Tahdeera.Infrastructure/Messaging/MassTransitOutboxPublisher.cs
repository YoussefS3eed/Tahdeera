using MassTransit;
using Microsoft.Extensions.Logging;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Interfaces.Messaging;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Messaging;

public sealed class MassTransitOutboxPublisher : IMessageOutboxPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MassTransitOutboxPublisher> _logger;

    public MassTransitOutboxPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<MassTransitOutboxPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<OutboxPublishResult> PublishAsync(MessageOutbox message, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new SendWhatsAppMessageCommand(
                OutboxMessageId: message.Id,
                WorkspaceId: message.WorkspaceId,
                RecipientPhoneE164: message.RecipientPhone,
                MessageType: message.MessageType,
                TemplateName: message.TemplateName,
                LanguageCode: message.LanguageCode,
                PayloadJson: message.Payload,
                DispatchedAt: DateTimeOffset.UtcNow);

            await _publishEndpoint.Publish(command, cancellationToken);
            return OutboxPublishResult.Succeeded(message.Id.ToString("N"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish outbox message {MessageId} to RabbitMQ", message.Id);
            return OutboxPublishResult.Failed("RABBITMQ_BROKER_UNAVAILABLE", ex.Message);
        }
    }
}
