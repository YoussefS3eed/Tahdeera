using MediatR;
using Microsoft.Extensions.Logging;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.WhatsApp.Commands.ProcessWhatsAppWebhook;

public sealed class ProcessWhatsAppWebhookCommandHandler : IRequestHandler<ProcessWhatsAppWebhookCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessWhatsAppWebhookCommandHandler> _logger;

    public ProcessWhatsAppWebhookCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ProcessWhatsAppWebhookCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ProcessWhatsAppWebhookCommand request, CancellationToken ct)
    {
        if (request.Payload?.Entry == null || request.Payload.Entry.Count == 0)
        {
            return;
        }

        bool hasChanges = false;

        foreach (var entry in request.Payload.Entry)
        {
            if (entry.Changes == null) continue;

            foreach (var change in entry.Changes)
            {
                if (change.Value?.Statuses == null) continue;

                foreach (var statusItem in change.Value.Statuses)
                {
                    var wamid = statusItem.Id;
                    if (string.IsNullOrWhiteSpace(wamid)) continue;

                    var outbox = await _unitOfWork.MessageOutbox.GetByProviderMessageIdAsync(wamid, ct);
                    if (outbox == null)
                    {
                        var maskedPhone = MaskPhoneNumber(statusItem.RecipientId);
                        _logger.LogWarning(
                            "Meta WhatsApp webhook status receipt received for unknown wamid: {Wamid}, recipient: {RecipientPhone}, status: {Status}. Skipping without error.",
                            wamid, maskedPhone, statusItem.Status);
                        continue;
                    }

                    var statusType = statusItem.Status?.Trim().ToLowerInvariant();
                    var eventTime = ParseTimestamp(statusItem.Timestamp);

                    switch (statusType)
                    {
                        case "delivered":
                            {
                                var transitioned = outbox.TransitionToDelivered(eventTime);
                                if (transitioned)
                                {
                                    var yearMonth = eventTime.UtcDateTime.ToString("yyyy-MM");
                                    var ledger = await _unitOfWork.WorkspaceWhatsAppUsageLedgers.GetOrCreateCurrentMonthAsync(outbox.WorkspaceId, yearMonth, ct);
                                    ledger?.RecordDelivered();
                                    hasChanges = true;
                                    _logger.LogInformation("Outbox message {OutboxId} (wamid: {Wamid}) marked Delivered.", outbox.Id, wamid);
                                }
                                else
                                {
                                    _logger.LogInformation("Outbox message {OutboxId} (wamid: {Wamid}) was already Delivered. Idempotent no-op.", outbox.Id, wamid);
                                }
                                break;
                            }

                        case "read":
                            {
                                outbox.RecordReadReceipt(eventTime);
                                hasChanges = true;
                                _logger.LogInformation("Outbox message {OutboxId} (wamid: {Wamid}) marked Read in metadata.", outbox.Id, wamid);
                                break;
                            }

                        case "failed":
                            {
                                if (outbox.Status != OutboxMessageStatus.Delivered)
                                {
                                    var err = statusItem.Errors?.FirstOrDefault();
                                    var errCode = err != null ? $"ERR_META_{err.Code}" : "ERR_META_DELIVERY_FAILED";
                                    var errDetails = err?.Title ?? err?.Message ?? err?.ErrorData?.Details ?? "Delivery failed reported by Meta";

                                    outbox.TransitionToFailedPermanent(errCode, errDetails);

                                    var yearMonth = eventTime.UtcDateTime.ToString("yyyy-MM");
                                    var ledger = await _unitOfWork.WorkspaceWhatsAppUsageLedgers.GetOrCreateCurrentMonthAsync(outbox.WorkspaceId, yearMonth, ct);
                                    ledger?.RecordFailed();
                                    hasChanges = true;

                                    _logger.LogWarning("Outbox message {OutboxId} (wamid: {Wamid}) delivery failed: {Code} - {Details}", outbox.Id, wamid, errCode, errDetails);
                                }
                                break;
                            }

                        case "sent":
                            {
                                if (outbox.Status != OutboxMessageStatus.Sent && outbox.Status != OutboxMessageStatus.Delivered)
                                {
                                    outbox.TransitionToSent(wamid, outbox.CostEgp ?? 0m);
                                    hasChanges = true;
                                }
                                break;
                            }

                        default:
                            _logger.LogDebug("Received unhandled WhatsApp webhook status '{Status}' for wamid {Wamid}", statusItem.Status, wamid);
                            break;
                    }
                }
            }
        }

        if (hasChanges)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    private static DateTimeOffset ParseTimestamp(string? timestampStr)
    {
        if (long.TryParse(timestampStr, out var unixSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }
        return DateTimeOffset.UtcNow;
    }

    private static string MaskPhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "****";
        var trimmed = phone.Trim();
        if (trimmed.Length <= 7) return "****";
        return $"{trimmed[..3]}****{trimmed[^4..]}";
    }
}
