using MediatR;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Groups.Commands.ChangeGroupStatus;

public sealed class ChangeGroupStatusCommandHandler : IRequestHandler<ChangeGroupStatusCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public ChangeGroupStatusCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ChangeGroupStatusCommand request, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetByIdForWorkspaceAsync(request.WorkspaceId, request.GroupId, cancellationToken);

        if (group is null)
        {
            return DomainErrors.Groups.NotFound;
        }

        group.SetStatus(request.NewStatus, request.ActorUserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
