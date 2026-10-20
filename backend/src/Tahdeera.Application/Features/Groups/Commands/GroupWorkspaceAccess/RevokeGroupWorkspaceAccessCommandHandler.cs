using MediatR;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Groups.Commands.GroupWorkspaceAccess;

public sealed class RevokeGroupWorkspaceAccessCommandHandler : IRequestHandler<RevokeGroupWorkspaceAccessCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public RevokeGroupWorkspaceAccessCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RevokeGroupWorkspaceAccessCommand request, CancellationToken cancellationToken)
    {
        var access = await _unitOfWork.GroupWorkspaceAccesses
            .FirstOrDefaultAsync(a => a.Id == request.AccessId, cancellationToken);

        if (access is null)
        {
            return Error.NotFound("ACCESS_NOT_FOUND", "لم يتم العثور على صلاحية الوصول المحددة.");
        }

        var group = await _unitOfWork.Groups.GetByIdForWorkspaceAsync(request.WorkspaceId, access.GroupId, cancellationToken);
        if (group is null)
        {
            return Error.NotFound("ACCESS_NOT_FOUND", "لم يتم العثور على صلاحية الوصول المحددة.");
        }

        if (access.RevokedAt != null)
        {
            return Result.Success();
        }

        access.Revoke(request.ActorUserId, DateTimeOffset.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
