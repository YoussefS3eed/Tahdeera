namespace Tahdeera.Application.Features.Students.DTOs;

public record RecentAttendanceSessionDto(
    Guid SessionId,
    string GroupName,
    DateTimeOffset SessionDate,
    string Status,
    DateTimeOffset? CheckInAt
);
