using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Groups.Commands.CreateGroup;

public sealed record CreateGroupCommand(
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
    AbsenceAction AbsenceAction,
    Guid ActorUserId,
    string? TargetGender = "CoEd",
    int? MaxCapacity = null) : IRequest<Result<GroupDto>>, ITransactionalRequest;
