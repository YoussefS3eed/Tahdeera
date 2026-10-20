using MediatR;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Groups.Commands.ChangeGroupStudentStatus;

public sealed class ChangeGroupStudentStatusCommandHandler : IRequestHandler<ChangeGroupStudentStatusCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public ChangeGroupStudentStatusCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ChangeGroupStudentStatusCommand request, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetByIdForWorkspaceAsync(request.WorkspaceId, request.GroupId, cancellationToken);

        if (group is null)
        {
            return DomainErrors.Groups.NotFound;
        }

        var groupStudent = await _unitOfWork.GroupStudents
            .FirstOrDefaultAsync(gs => gs.GroupId == request.GroupId && gs.StudentId == request.StudentId, cancellationToken);

        if (groupStudent is null)
        {
            return DomainErrors.Groups.StudentNotEnrolled;
        }

        if (groupStudent.Status == request.NewStatus)
        {
            return Result.Success();
        }

        var now = DateTimeOffset.UtcNow;
        var oldStatus = groupStudent.Status;

        switch (request.NewStatus)
        {
            case GroupStudentStatus.Withdrawn:
                groupStudent.Withdraw(now, request.ActorUserId);
                break;
            case GroupStudentStatus.Suspended:
                groupStudent.Suspend(request.ActorUserId);
                break;
            case GroupStudentStatus.Active:
                groupStudent.Reinstate(request.ActorUserId);
                break;
        }

        await _unitOfWork.GroupStudentStatusHistories.AddAsync(GroupStudentStatusHistory.Record(
            groupStudent.Id,
            oldStatus,
            request.NewStatus,
            now,
            request.ActorUserId,
            request.Reason), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
