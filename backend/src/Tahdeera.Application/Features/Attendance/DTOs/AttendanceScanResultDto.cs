namespace Tahdeera.Application.Features.Attendance.DTOs;

public sealed record AttendanceScanResultDto(
    Guid AttendanceRecordId,
    Guid SessionId,
    Guid StudentId,
    string StudentName,
    string StudentCode,
    string Status,
    DateTimeOffset CheckInAt,
    bool IsGuestMakeup,
    Guid? HomeGroupId,
    bool NotificationStaged,
    long LatencyMs);
