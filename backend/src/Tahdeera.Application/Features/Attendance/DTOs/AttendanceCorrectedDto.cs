namespace Tahdeera.Application.Features.Attendance.DTOs;

public sealed record AttendanceCorrectedDto(
    Guid AttendanceRecordId,
    Guid StudentId,
    string OldStatus,
    string NewStatus,
    bool AutoEvaluationLocked,
    DateTimeOffset LastModifiedAt,
    Guid LastModifiedBy,
    int ConsecutiveUnexcusedAbsences,
    bool SuspensionReconciled,
    bool NotificationStaged);
