using MediatR;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Sessions.Commands.CancelSession;

public sealed class CancelSessionCommandHandler : IRequestHandler<CancelSessionCommand, Result<SessionCancelledDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelSessionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SessionCancelledDto>> Handle(CancelSessionCommand request, CancellationToken cancellationToken)
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

        if (session.Status == SessionStatus.Cancelled)
        {
            return Result.Success(new SessionCancelledDto(
                SessionId: session.Id,
                CancelledAt: session.CancelledAt ?? DateTimeOffset.UtcNow,
                CancellationReason: session.CancellationReason ?? string.Empty,
                VoidedAttendanceRecordsCount: 0));
        }

        var now = DateTimeOffset.UtcNow;
        session.Cancel(request.CancellationReason, request.ActorUserId, now);

        var voidedCount = 0;
        foreach (var record in session.AttendanceRecords.Where(r => r.VoidedAt == null))
        {
            record.Void(request.CancellationReason, now);

            await _unitOfWork.AttendanceHistories.AddAsync(AttendanceHistory.Create(
                attendanceRecordId: record.Id,
                oldStatus: record.Status,
                newStatus: record.Status,
                oldSource: record.Source,
                newSource: record.Source,
                changedByUserId: request.ActorUserId,
                changedAt: now,
                reason: request.CancellationReason,
                changeKind: AttendanceChangeKind.VoidedOnCancellation), cancellationToken);

            voidedCount++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SessionCancelledDto(
            SessionId: session.Id,
            CancelledAt: now,
            CancellationReason: request.CancellationReason,
            VoidedAttendanceRecordsCount: voidedCount));
    }
}
