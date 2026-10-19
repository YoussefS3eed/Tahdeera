namespace Tahdeera.Application.Features.Students.DTOs;

public record AttendanceSummaryDto(
    int TotalSessions,
    int AttendedCount,
    int LateCount,
    int AbsentCount,
    double AttendancePercentage
);
