namespace Tahdeera.Application.Features.Attendance.DTOs;

public sealed record StudentAttendanceHistoryDto(
    Guid StudentId,
    string StudentName,
    string StudentCode,
    int TotalSessions,
    int AttendedCount,
    int LateCount,
    int AbsentCount,
    int ExcusedCount,
    double AttendancePercentage,
    IReadOnlyList<StudentAttendanceHistoryItemDto> Timeline);
