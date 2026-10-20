using MediatR;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Sessions.Commands.UpdateSessionSchedule;

public sealed class UpdateSessionScheduleCommandHandler : IRequestHandler<UpdateSessionScheduleCommand, Result<SessionScheduleUpdatedDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSessionScheduleCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SessionScheduleUpdatedDto>> Handle(UpdateSessionScheduleCommand request, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.Sessions.GetByIdWithDetailsAsync(request.SessionId, cancellationToken);

        if (session is null)
        {
            return DomainErrors.Sessions.NotFound;
        }

        if (session.Group.OwnerWorkspaceId != request.WorkspaceId)
        {
            return DomainErrors.Sessions.UnauthorizedWorkspaceAccess;
        }

        if (session.Status != SessionStatus.Scheduled)
        {
            return DomainErrors.Sessions.AlreadyCompletedScheduleFrozen;
        }

        if (request.EndsAt <= request.StartsAt)
        {
            return DomainErrors.Sessions.InvalidTimeRange;
        }

        session.UpdateSchedule(request.StartsAt, request.EndsAt, request.LateGraceMinutes, request.ActorUserId);

        var reevaluatedCount = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var record in session.AttendanceRecords)
        {
            if (record.AutoEvaluationLocked || !record.CheckInAt.HasValue || record.VoidedAt != null)
            {
                continue;
            }

            var newStatus = session.EvaluatePunctuality(record.CheckInAt.Value);
            if (record.Status != newStatus)
            {
                var oldStatus = record.Status;
                var oldSource = record.Source;
                record.Status = newStatus;
                record.LastModifiedAt = now;
                record.LastModifiedByUserId = request.ActorUserId;

                await _unitOfWork.AttendanceHistories.AddAsync(AttendanceHistory.Create(
                    attendanceRecordId: record.Id,
                    oldStatus: oldStatus,
                    newStatus: newStatus,
                    oldSource: oldSource,
                    newSource: record.Source,
                    changedByUserId: request.ActorUserId,
                    changedAt: now,
                    reason: "Dynamic schedule re-evaluation",
                    changeKind: AttendanceChangeKind.SystemReevaluation), cancellationToken);

                reevaluatedCount++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SessionScheduleUpdatedDto(
            SessionId: session.Id,
            StartsAt: session.StartsAt,
            EndsAt: session.EndsAt,
            LateGraceMinutes: session.LateGraceMinutes,
            ReevaluatedRecordsCount: reevaluatedCount));
    }
}
