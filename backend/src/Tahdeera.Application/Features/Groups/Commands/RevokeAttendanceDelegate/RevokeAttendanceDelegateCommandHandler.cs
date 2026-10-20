using MediatR;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Groups.Commands.RevokeAttendanceDelegate;

public sealed class RevokeAttendanceDelegateCommandHandler : IRequestHandler<RevokeAttendanceDelegateCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public RevokeAttendanceDelegateCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RevokeAttendanceDelegateCommand request, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetByIdForWorkspaceAsync(request.WorkspaceId, request.GroupId, cancellationToken);

        if (group is null)
        {
            return DomainErrors.Groups.NotFound;
        }

        var delegation = await _unitOfWork.GroupAttendanceDelegations
            .FirstOrDefaultAsync(d => d.Id == request.DelegationId && d.GroupId == request.GroupId, cancellationToken);

        if (delegation is null)
        {
            return DomainErrors.Groups.DelegationNotFound;
        }

        if (delegation.RevokedAt != null)
        {
            return Result.Success();
        }

        delegation.Revoke(request.ActorUserId, request.Reason, DateTimeOffset.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
