namespace Tahdeera.Application.Features.Attendance.DTOs;

public sealed record StudentAttendanceHistoryItemDto(
    Guid AttendanceRecordId,
    Guid SessionId,
    Guid GroupId,
    string GroupName,
    DateTimeOffset SessionDate,
    string Status,
    DateTimeOffset? CheckInAt,
    bool IsGuestMakeup);
