namespace Tahdeera.Application.Features.Sessions.DTOs;

public sealed record SessionDto(
    Guid Id,
    Guid GroupId,
    Guid WorkspaceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int LateGraceMinutes,
    string? RoomName,
    string? Topic,
    string Status,
    DateTimeOffset CreatedAt);
