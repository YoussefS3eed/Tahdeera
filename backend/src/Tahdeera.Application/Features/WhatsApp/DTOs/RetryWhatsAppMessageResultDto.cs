namespace Tahdeera.Application.Features.WhatsApp.DTOs;

public sealed record RetryWhatsAppMessageResultDto(
    Guid MessageId,
    Guid WorkspaceId,
    string Status,
    string RefreshedRecipientPhone,
    DateTimeOffset? NextAttemptAt,
    string Message);
