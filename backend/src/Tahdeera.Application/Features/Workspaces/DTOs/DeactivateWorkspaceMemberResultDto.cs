namespace Tahdeera.Application.Features.Workspaces.DTOs;

public record DeactivateWorkspaceMemberResultDto(
    Guid MemberId,
    bool IsActive,
    DateTimeOffset LeftAt);
