namespace Tahdeera.Application.Features.Attendance.DTOs;

public sealed record SessionAttendanceSheetDto(
    Guid SessionId,
    Guid GroupId,
    string GroupName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int TotalEnrolled,
    int PresentCount,
    int LateCount,
    int AbsentCount,
    int ExcusedCount,
    int GuestCount,
    IReadOnlyList<AttendanceSheetItemDto> Items);
