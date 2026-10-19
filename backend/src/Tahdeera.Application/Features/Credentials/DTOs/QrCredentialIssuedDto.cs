namespace Tahdeera.Application.Features.Credentials.DTOs;

public record QrCredentialIssuedDto(
    Guid CredentialId,
    Guid StudentId,
    string RawPayload,
    DateTimeOffset IssuedAt,
    Guid? ReplacedCredentialId
);
