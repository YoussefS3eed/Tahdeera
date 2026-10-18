namespace Tahdeera.Application.Features.Workspaces.DTOs;

public record CreateOwnerDto(
    string Username,
    string Email,
    string Password,
    string DisplayName,
    string? Phone);
