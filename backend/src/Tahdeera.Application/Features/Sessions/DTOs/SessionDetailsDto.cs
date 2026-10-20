namespace Tahdeera.Application.Features.Sessions.DTOs;

public sealed record SessionDetailsDto(
    Guid Id,
    Guid GroupId,
    string GroupName,
    Guid WorkspaceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int LateGraceMinutes,
    string? RoomName,
    string? Topic,
    string Status,
    DateTimeOffset? AutoAbsenceProcessedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    int PresentCount,
    int LateCount,
    int AbsentCount,
    int ExcusedCount,
    int TotalRosterCount);
