using Mapster;
using MediatR;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Sessions.Commands.ScheduleSession;

public sealed class ScheduleSessionCommandHandler : IRequestHandler<ScheduleSessionCommand, Result<SessionDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ScheduleSessionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SessionDto>> Handle(ScheduleSessionCommand request, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetByIdAsync(request.GroupId, cancellationToken);

        if (group is null)
        {
            return DomainErrors.Groups.NotFound;
        }

        if (group.OwnerWorkspaceId != request.WorkspaceId)
        {
            return DomainErrors.Sessions.UnauthorizedWorkspaceAccess;
        }

        if (group.Status == GroupStatus.Archived)
        {
            return DomainErrors.Groups.ArchivedCannotSchedule;
        }

        if (request.EndsAt <= request.StartsAt)
        {
            return DomainErrors.Sessions.InvalidTimeRange;
        }

        var graceMinutes = request.LateGraceMinutes ?? group.LateGraceMinutes;

        var session = Session.Create(
            groupId: group.Id,
            startsAt: request.StartsAt,
            endsAt: request.EndsAt,
            lateGraceMinutes: graceMinutes,
            createdByUserId: request.ActorUserId);

        await _unitOfWork.Sessions.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = session.Adapt<SessionDto>() with
        {
            WorkspaceId = group.OwnerWorkspaceId,
            RoomName = request.RoomName ?? group.RoomName,
            Topic = request.Topic
        };

        return Result.Success(dto);
    }
}
