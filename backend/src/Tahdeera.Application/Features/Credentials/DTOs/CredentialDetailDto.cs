namespace Tahdeera.Application.Features.Credentials.DTOs;

public record CredentialDetailDto(
    Guid Id,
    string CredentialType,
    string? CardUidDisplay,
    string? Label,
    DateTimeOffset IssuedAt,
    string? IssuedByUserName,
    DateTimeOffset? RevokedAt,
    string? RevokedByUserName,
    string? RevocationReason,
    bool IsActive
);
