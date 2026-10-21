namespace Tahdeera.Application.Features.WhatsApp.DTOs;

public sealed record WhatsAppMessageDetailDto(
    Guid Id,
    Guid WorkspaceId,
    Guid? StudentId,
    string? StudentName,
    Guid? SessionId,
    string RecipientPhone,
    string MessageType,
    string TemplateName,
    string LanguageCode,
    string Payload,
    string Status,
    string SenderMode,
    short Attempts,
    short MaxAttempts,
    DateTimeOffset? SentAt,
    DateTimeOffset? DeliveredAt,
    string? ErrorCode,
    string? ErrorDetails,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
