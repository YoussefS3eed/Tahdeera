namespace Tahdeera.Application.Features.Attendance.DTOs;

public sealed record GuestMakeupResultDto(
    Guid AttendanceRecordId,
    Guid SessionId,
    Guid StudentId,
    string StudentName,
    string Status,
    DateTimeOffset CheckInAt,
    bool IsGuestMakeup,
    Guid HomeGroupId,
    string HomeGroupName,
    bool HomeSessionExcused,
    Guid? ExcusedHomeSessionId,
    string BillingStatus,
    int GuestInvoicesCreated,
    bool NotificationStaged);
