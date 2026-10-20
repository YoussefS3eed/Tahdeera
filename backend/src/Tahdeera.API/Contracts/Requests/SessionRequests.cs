namespace Tahdeera.API.Contracts.Requests;

public sealed record ScheduleSessionRequest(
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? LateGraceMinutes = null,
    string? RoomName = null,
    string? Topic = null);

public sealed record UpdateSessionScheduleRequest(
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int LateGraceMinutes = 20);

public sealed record CancelSessionRequest(
    string CancellationReason);
