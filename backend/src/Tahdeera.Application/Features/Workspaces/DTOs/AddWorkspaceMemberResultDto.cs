namespace Tahdeera.Application.Features.Workspaces.DTOs;

public record AddWorkspaceMemberResultDto(
    Guid MemberId,
    Guid UserId,
    string DisplayName,
    string Role,
    bool IsActive,
    DateTimeOffset JoinedAt);
