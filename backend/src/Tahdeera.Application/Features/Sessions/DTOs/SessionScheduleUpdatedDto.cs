namespace Tahdeera.Application.Features.Sessions.DTOs;

public sealed record SessionScheduleUpdatedDto(
    Guid SessionId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int LateGraceMinutes,
    int ReevaluatedRecordsCount);
