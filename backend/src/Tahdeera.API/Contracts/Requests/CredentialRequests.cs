namespace Tahdeera.API.Contracts.Requests;

public record IssueNfcCredentialRequest(
    string Token,
    string? CardUidDisplay
);

public record RevokeCredentialRequest(
    string Reason
);
