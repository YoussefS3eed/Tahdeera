namespace Tahdeera.Application.Features.Workspaces.DTOs;

public record WorkspaceMemberDto(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string Email,
    string? Phone,
    string Role,
    bool IsActive,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LeftAt);
