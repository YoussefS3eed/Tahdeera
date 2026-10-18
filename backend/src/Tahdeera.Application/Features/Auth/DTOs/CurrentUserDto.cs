namespace Tahdeera.Application.Features.Auth.DTOs;

public record CurrentUserDto(
    UserProfileDto User,
    IReadOnlyList<UserWorkspaceMembershipDto> Workspaces);
