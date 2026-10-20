namespace Tahdeera.Application.Features.Attendance.DTOs;

public sealed record AttendanceSheetItemDto(
    Guid AttendanceRecordId,
    Guid StudentId,
    string StudentName,
    string StudentCode,
    string Status,
    DateTimeOffset? CheckInAt,
    bool AutoEvaluationLocked,
    bool IsGuestMakeup,
    Guid? HomeGroupId,
    string? HomeGroupName,
    string? RetroactiveReason);
