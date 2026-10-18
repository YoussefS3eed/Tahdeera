namespace Tahdeera.API.Contracts.Requests;

public record AddWorkspaceMemberRequest(
    string Username,
    string Email,
    string Password,
    string DisplayName,
    string? Phone,
    string Role);
