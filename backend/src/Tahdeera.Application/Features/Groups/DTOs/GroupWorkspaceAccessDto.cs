namespace Tahdeera.Application.Features.Groups.DTOs;

public sealed record GroupWorkspaceAccessDto(
    Guid Id,
    Guid GroupId,
    Guid GranteeWorkspaceId,
    string GranteeWorkspaceName,
    bool CanViewRoster,
    bool CanRegisterAttendance,
    bool CanEditAttendance,
    DateTimeOffset GrantedAt,
    DateTimeOffset? ExpiresAt);
