using Mapster;
using MediatR;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Sessions.Queries.GetGroupSessions;

public sealed class GetGroupSessionsQueryHandler : IRequestHandler<GetGroupSessionsQuery, Result<PagedList<SessionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetGroupSessionsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedList<SessionDto>>> Handle(GetGroupSessionsQuery request, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetByIdAsync(request.GroupId, cancellationToken);

        if (group is null)
        {
            return DomainErrors.Groups.NotFound;
        }

        var hasAccess = group.OwnerWorkspaceId == request.WorkspaceId
            || await _unitOfWork.GroupWorkspaceAccesses
                .AnyAsync(a => a.GroupId == request.GroupId
                            && a.WorkspaceId == request.WorkspaceId
                            && a.RevokedAt == null
                            && a.CanViewRoster, cancellationToken);

        if (!hasAccess)
        {
            return DomainErrors.Sessions.UnauthorizedWorkspaceAccess;
        }

        var sessions = await _unitOfWork.Sessions.GetSessionsForGroupAsync(
            request.GroupId,
            request.Status,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        var totalCount = sessions.Count;

        var pagedSessions = sessions
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var dtos = pagedSessions.Select(s => s.Adapt<SessionDto>() with
        {
            WorkspaceId = group.OwnerWorkspaceId,
            RoomName = group.RoomName
        }).ToList();

        var result = new PagedList<SessionDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Result.Success(result);
    }
}
