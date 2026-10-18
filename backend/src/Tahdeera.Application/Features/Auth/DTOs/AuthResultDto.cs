namespace Tahdeera.Application.Features.Auth.DTOs;

public record AuthResultDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    UserProfileDto User,
    IReadOnlyList<UserWorkspaceMembershipDto> Workspaces);
