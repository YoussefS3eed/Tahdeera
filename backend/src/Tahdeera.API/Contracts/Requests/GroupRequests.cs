using Tahdeera.Domain.Enums;

namespace Tahdeera.API.Contracts.Requests;

public sealed record CreateGroupRequest(
    Guid AcademicYearId,
    short GradeId,
    Guid? SubjectId,
    string Name,
    string? RoomName,
    string? ScheduleSummary,
    int LateGraceMinutes = 20,
    decimal DefaultMonthlyFee = 0.00m,
    short? ConsecutiveAbsenceThreshold = null,
    AbsenceAction AbsenceAction = AbsenceAction.NotifyOnly,
    string? TargetGender = "CoEd",
    int? MaxCapacity = null,
    Guid? ResponsibleTeacherUserId = null);

public sealed record UpdateGroupRequest(
    string Name,
    string? RoomName,
    string? ScheduleSummary,
    int LateGraceMinutes = 20,
    decimal DefaultMonthlyFee = 0.00m,
    short? ConsecutiveAbsenceThreshold = null,
    AbsenceAction AbsenceAction = AbsenceAction.NotifyOnly,
    string? TargetGender = "CoEd",
    int? MaxCapacity = null,
    bool RecalculateNow = false);

public sealed record ChangeGroupStatusRequest(GroupStatus Status);

public sealed record EnrollStudentRequest(Guid StudentId);

public sealed record WithdrawStudentRequest(string? Reason = null);

public sealed record ChangeGroupStudentStatusRequest(GroupStudentStatus NewStatus, string Reason);

public sealed record AssignAttendanceDelegateRequest(Guid AssistantUserId);

public sealed record RevokeAttendanceDelegateRequest(Guid DelegationId, string Reason);

public sealed record GrantGroupWorkspaceAccessRequest(
    Guid GranteeWorkspaceId,
    bool CanViewRoster = true,
    bool CanRegisterAttendance = false,
    bool CanEditAttendance = false,
    DateTimeOffset? ExpiresAt = null,
    string? Notes = null);
