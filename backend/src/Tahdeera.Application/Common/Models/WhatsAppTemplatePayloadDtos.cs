namespace Tahdeera.Application.Common.Models;

public sealed record AttendanceCheckInPayloadDto(
    string StudentName,
    string AttendanceStatus,
    string GroupName,
    string CheckInTime);

public sealed record AbsenceAlertPayloadDto(
    string StudentName,
    string GroupName,
    string SessionDate);

public sealed record AttendanceCorrectionPayloadDto(
    string StudentName,
    string OldStatus,
    string NewStatus,
    string GroupName,
    string CorrectionTime);
