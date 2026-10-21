namespace Tahdeera.Application.Features.WhatsApp.DTOs;

public sealed record WhatsAppMessageListItemDto(
    Guid Id,
    Guid? StudentId,
    string? StudentName,
    string RecipientPhone,
    string MessageType,
    string TemplateName,
    string LanguageCode,
    string Status,
    short Attempts,
    DateTimeOffset? SentAt,
    DateTimeOffset? DeliveredAt,
    decimal? CostEgp,
    string? ErrorCode,
    string? ErrorDetails,
    DateTimeOffset CreatedAt);
