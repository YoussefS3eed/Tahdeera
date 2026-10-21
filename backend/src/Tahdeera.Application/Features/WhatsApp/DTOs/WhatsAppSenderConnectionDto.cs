namespace Tahdeera.Application.Features.WhatsApp.DTOs;

public sealed record WhatsAppSenderConnectionDto(
    Guid Id,
    string WabaId,
    string PhoneNumberId,
    string DisplayPhoneNumber,
    string? VerifiedBusinessName,
    string Status,
    DateTimeOffset? ConnectedAt,
    bool HasAccessToken);
