namespace Tahdeera.Application.Features.Groups.DTOs;

public sealed record GroupDto(
    Guid Id,
    Guid WorkspaceId,
    Guid AcademicYearId,
    short GradeId,
    Guid? SubjectId,
    string Name,
    string? RoomName,
    string? ScheduleSummary,
    int LateGraceMinutes,
    decimal DefaultMonthlyFee,
    short? ConsecutiveAbsenceThreshold,
    string AbsenceAction,
    string Status,
    string TargetGender,
    int? MaxCapacity,
    int ActiveStudentCount,
    DateTimeOffset CreatedAt);
