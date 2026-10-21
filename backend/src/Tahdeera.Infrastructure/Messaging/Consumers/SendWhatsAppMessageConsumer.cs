using MassTransit;
using Microsoft.Extensions.Logging;
using Tahdeera.Application.Common.Interfaces.Messaging;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;
using Tahdeera.Infrastructure.Services;

namespace Tahdeera.Infrastructure.Messaging.Consumers;

public sealed class SendWhatsAppMessageConsumer : IConsumer<SendWhatsAppMessageCommand>
{
    private const decimal UtilityCostUsd = 0.0356m;
    private const decimal UtilityCostEgp = 1.3500m;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppSenderResolver _senderResolver;
    private readonly IMetaWhatsAppCloudApiClient _metaApiClient;
    private readonly ILogger<SendWhatsAppMessageConsumer> _logger;

    public SendWhatsAppMessageConsumer(
        IUnitOfWork unitOfWork,
        IWhatsAppSenderResolver senderResolver,
        IMetaWhatsAppCloudApiClient metaApiClient,
        ILogger<SendWhatsAppMessageConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _senderResolver = senderResolver;
        _metaApiClient = metaApiClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SendWhatsAppMessageCommand> context)
    {
        var cmd = context.Message;
        var ct = context.CancellationToken;

        // 1. Idempotency Gate
        var outbox = await _unitOfWork.MessageOutbox.GetByIdAsync(cmd.OutboxMessageId, ct);
        if (outbox is null)
        {
            _logger.LogWarning("Outbox message {OutboxId} not found in database. Skipping duplicate broker delivery.", cmd.OutboxMessageId);
            return;
        }

        if (outbox.Status is OutboxMessageStatus.Sent or OutboxMessageStatus.Delivered or OutboxMessageStatus.Cancelled)
        {
            _logger.LogInformation("Outbox message {OutboxId} already in terminal status {Status}. Skipping delivery.",
                cmd.OutboxMessageId, outbox.Status);
            return;
        }

        if (outbox.Status == OutboxMessageStatus.FailedPermanent)
        {
            _logger.LogWarning("Outbox message {OutboxId} marked FailedPermanent. Skipping delivery.", cmd.OutboxMessageId);
            return;
        }

        if (!string.IsNullOrWhiteSpace(outbox.ProviderMessageId))
        {
            _logger.LogInformation("Outbox message {OutboxId} already has provider ID {ProviderId}. Skipping delivery.",
                cmd.OutboxMessageId, outbox.ProviderMessageId);
            return;
        }

        // 2. Resolve Active Sender Credentials (Zero Silent Fallback - CONF-07)
        var sender = await _senderResolver.ResolveSenderAsync(cmd.WorkspaceId, ct);
        if (sender is null || !sender.CanSend)
        {
            var reason = sender?.RejectionReason ?? "WhatsApp sender is not configured or disabled.";
            _logger.LogError("Sender resolution failed for workspace {WorkspaceId}: {Reason}. Marking message {OutboxId} FailedPermanent (CONF-07 Zero Silent Fallback).",
                cmd.WorkspaceId, reason, cmd.OutboxMessageId);

            outbox.TransitionToFailedPermanent("ERR_WA_NO_SENDER_CONFIGURED", reason);
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        // 3. Record Attempt Metadata
        outbox.RecordAttempt(sender.SenderMode, sender.PhoneNumberId);

        // 4. Dispatch Meta Template Message via HTTP Client
        try
        {
            var response = await _metaApiClient.SendTemplateMessageAsync(
                sender,
                cmd.RecipientPhoneE164,
                cmd.TemplateName,
                cmd.LanguageCode,
                cmd.PayloadJson,
                ct);

            if (response.IsSuccess)
            {
                outbox.TransitionToSent(response.ProviderMessageId!, UtilityCostUsd, UtilityCostEgp, DateTimeOffset.UtcNow);
                _logger.LogInformation("Outbox message {OutboxId} delivered via Meta. Provider ID: {ProviderId}",
                    cmd.OutboxMessageId, response.ProviderMessageId);
            }
            else if (response.IsTransient)
            {
                if (outbox.AttemptCount < 5)
                {
                    var delay = WhatsAppRetryPolicy.GetRetryDelay(outbox.AttemptCount);
                    var nextAttemptAt = DateTimeOffset.UtcNow.Add(delay);

                    outbox.ScheduleRetryAt(nextAttemptAt, response.ErrorCode, response.ErrorMessage);
                    _logger.LogWarning("Outbox message {OutboxId} transient error {Code}: {Message}. Retrying at {NextAttempt}",
                        cmd.OutboxMessageId, response.ErrorCode, response.ErrorMessage, nextAttemptAt);
                }
                else
                {
                    outbox.TransitionToFailedPermanent("ERR_MAX_ATTEMPTS_EXCEEDED", $"Exceeded maximum 5 delivery attempts. Last error: {response.ErrorMessage}");
                    _logger.LogError("Outbox message {OutboxId} exceeded max retry attempts. Marking FailedPermanent.", cmd.OutboxMessageId);
                }
            }
            else
            {
                // Permanent Failure (131026 phone not on WA, 132001 template paused, auth expired)
                outbox.TransitionToFailedPermanent(response.ErrorCode ?? "ERR_WA_PERMANENT", response.ErrorMessage ?? "Permanent failure");
                _logger.LogError("Outbox message {OutboxId} encountered permanent error {Code}: {Message}. Halting retries.",
                    cmd.OutboxMessageId, response.ErrorCode, response.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception processing outbox message {OutboxId}", cmd.OutboxMessageId);
            if (outbox.AttemptCount < 5)
            {
                var delay = WhatsAppRetryPolicy.GetRetryDelay(outbox.AttemptCount);
                outbox.ScheduleRetryAt(DateTimeOffset.UtcNow.Add(delay), "ERR_UNHANDLED_EXCEPTION", ex.Message);
            }
            else
            {
                outbox.TransitionToFailedPermanent("ERR_MAX_ATTEMPTS_EXCEEDED", ex.Message);
            }
        }

        // 5. Persist State Transition
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
