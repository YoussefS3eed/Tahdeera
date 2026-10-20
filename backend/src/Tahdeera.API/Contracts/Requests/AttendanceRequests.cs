using Tahdeera.Domain.Enums;

namespace Tahdeera.API.Contracts.Requests;

public sealed record ScanCardAttendanceRequest(string RawToken);

public sealed record ManualCheckInRequest(
    Guid StudentId,
    string? RetroactiveReason = null);

public sealed record RegisterGuestMakeupAttendanceRequest(
    Guid StudentId,
    Guid HomeGroupId,
    Guid? HomeSessionId = null,
    string? Reason = null);

public sealed record CorrectAttendanceRecordRequest(
    AttendanceStatus NewStatus,
    string Reason);
