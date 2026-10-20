using Mapster;
using MediatR;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;
using GroupWorkspaceAccessEntity = Tahdeera.Domain.Entities.GroupWorkspaceAccess;

namespace Tahdeera.Application.Features.Groups.Commands.GroupWorkspaceAccess;

public sealed class GrantGroupWorkspaceAccessCommandHandler : IRequestHandler<GrantGroupWorkspaceAccessCommand, Result<GroupWorkspaceAccessDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GrantGroupWorkspaceAccessCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GroupWorkspaceAccessDto>> Handle(GrantGroupWorkspaceAccessCommand request, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetByIdForWorkspaceAsync(request.WorkspaceId, request.GroupId, cancellationToken);

        if (group is null)
        {
            return DomainErrors.Groups.NotFound;
        }

        var granteeWorkspace = await _unitOfWork.Workspaces.GetByIdAsync(request.GranteeWorkspaceId, cancellationToken);

        if (granteeWorkspace is null)
        {
            return Error.NotFound("WORKSPACE_NOT_FOUND", "مساحة العمل المستفيدة غير موجودة.");
        }

        var existingAccess = await _unitOfWork.GroupWorkspaceAccesses
            .FirstOrDefaultAsync(a => a.GroupId == request.GroupId
                                   && a.WorkspaceId == request.GranteeWorkspaceId
                                   && a.RevokedAt == null, cancellationToken);

        GroupWorkspaceAccessEntity access;
        if (existingAccess is not null)
        {
            existingAccess.CanViewRoster = request.CanViewRoster;
            existingAccess.CanRegisterAttendance = request.CanRegisterAttendance;
            existingAccess.CanEditAttendance = request.CanEditAttendance;
            existingAccess.Notes = request.Notes?.Trim();
            access = existingAccess;
        }
        else
        {
            access = GroupWorkspaceAccessEntity.Grant(
                groupId: request.GroupId,
                workspaceId: request.GranteeWorkspaceId,
                grantedByUserId: request.ActorUserId,
                canViewRoster: request.CanViewRoster,
                canRegisterAttendance: request.CanRegisterAttendance,
                canEditAttendance: request.CanEditAttendance,
                notes: request.Notes);

            await _unitOfWork.GroupWorkspaceAccesses.AddAsync(access, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = access.Adapt<GroupWorkspaceAccessDto>() with
        {
            GranteeWorkspaceName = granteeWorkspace.Name
        };

        return Result.Success(dto);
    }
}
