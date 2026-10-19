namespace Tahdeera.Application.Features.Students.DTOs;

public record CredentialSummaryDto(
    Guid CredentialId,
    string CredentialType,
    string? CardUidDisplay,
    bool IsActive,
    DateTimeOffset IssuedAt,
    DateTimeOffset? RevokedAt,
    string? RevocationReason
);
