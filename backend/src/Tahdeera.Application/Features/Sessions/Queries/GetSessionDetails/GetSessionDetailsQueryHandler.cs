using Mapster;
using MediatR;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Sessions.Queries.GetSessionDetails;

public sealed class GetSessionDetailsQueryHandler : IRequestHandler<GetSessionDetailsQuery, Result<SessionDetailsDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSessionDetailsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SessionDetailsDto>> Handle(GetSessionDetailsQuery request, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.Sessions.GetByIdWithDetailsAsync(request.SessionId, cancellationToken);

        if (session is null)
        {
            return DomainErrors.Sessions.NotFound;
        }

        var hasAccess = session.Group.OwnerWorkspaceId == request.WorkspaceId
            || await _unitOfWork.GroupWorkspaceAccesses
                .AnyAsync(a => a.GroupId == session.GroupId
                            && a.WorkspaceId == request.WorkspaceId
                            && a.RevokedAt == null
                            && a.CanViewRoster, cancellationToken);

        if (!hasAccess)
        {
            return DomainErrors.Sessions.UnauthorizedWorkspaceAccess;
        }

        var totalRosterCount = await _unitOfWork.GroupStudents
            .CountAsync(gs => gs.GroupId == session.GroupId && gs.Status == GroupStudentStatus.Active, cancellationToken);

        var dto = session.Adapt<SessionDetailsDto>() with
        {
            TotalRosterCount = totalRosterCount,
            RoomName = session.Group.RoomName
        };

        return Result.Success(dto);
    }
}
