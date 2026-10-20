using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Groups.Commands.UpdateGroup;

public sealed record UpdateGroupCommand(
    Guid WorkspaceId,
    Guid GroupId,
    string Name,
    string? RoomName,
    string? ScheduleSummary,
    int LateGraceMinutes,
    decimal DefaultMonthlyFee,
    short? ConsecutiveAbsenceThreshold,
    AbsenceAction AbsenceAction,
    Guid ActorUserId,
    string? TargetGender = "CoEd",
    int? MaxCapacity = null,
    bool RecalculateNow = false) : IRequest<Result<GroupDto>>, ITransactionalRequest;
