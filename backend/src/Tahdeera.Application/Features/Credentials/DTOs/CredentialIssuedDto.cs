namespace Tahdeera.Application.Features.Credentials.DTOs;

public record CredentialIssuedDto(
    Guid CredentialId,
    Guid StudentId,
    string CredentialType,
    string? CardUidDisplay,
    DateTimeOffset IssuedAt,
    Guid? ReplacedCredentialId
);
