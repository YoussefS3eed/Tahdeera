using MediatR;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Groups.Commands.WithdrawStudentFromGroup;

public sealed class WithdrawStudentFromGroupCommandHandler : IRequestHandler<WithdrawStudentFromGroupCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public WithdrawStudentFromGroupCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(WithdrawStudentFromGroupCommand request, CancellationToken cancellationToken)
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

        if (groupStudent.Status == GroupStudentStatus.Withdrawn)
        {
            return Result.Success();
        }

        var now = DateTimeOffset.UtcNow;
        var oldStatus = groupStudent.Status;
        groupStudent.Withdraw(now, request.ActorUserId);

        await _unitOfWork.GroupStudentStatusHistories.AddAsync(GroupStudentStatusHistory.Record(
            groupStudent.Id,
            oldStatus,
            GroupStudentStatus.Withdrawn,
            now,
            request.ActorUserId,
            request.Reason ?? "Student withdrawn from group"), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
