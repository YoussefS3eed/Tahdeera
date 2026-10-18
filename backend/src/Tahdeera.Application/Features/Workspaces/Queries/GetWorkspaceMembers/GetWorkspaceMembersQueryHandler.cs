using MediatR;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Workspaces.Queries.GetWorkspaceMembers;

public class GetWorkspaceMembersQueryHandler : IRequestHandler<GetWorkspaceMembersQuery, Result<List<WorkspaceMemberDto>>>
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetWorkspaceMembersQueryHandler(
        ITahdeeraDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<WorkspaceMemberDto>>> Handle(GetWorkspaceMembersQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Error.Unauthorized("AUTH_UNAUTHORIZED", "User is not authenticated.");
        }

        if (!_currentUserService.IsPlatformAdmin)
        {
            var isMember = await _dbContext.WorkspaceMembers
                .AsNoTracking()
                .AnyAsync(m => m.WorkspaceId == request.WorkspaceId && m.UserId == currentUserId.Value && m.IsActive, cancellationToken);

            if (!isMember)
            {
                return Error.Forbidden("UNAUTHORIZED_WORKSPACE_ACCESS", "ليس لديك صلاحية الوصول إلى مساحة العمل هذه.");
            }
        }

        var members = await _dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == request.WorkspaceId)
            .Include(m => m.User)
            .OrderByDescending(m => m.IsActive)
            .ThenBy(m => m.JoinedAt)
            .Select(m => new WorkspaceMemberDto(
                m.Id,
                m.UserId,
                m.User != null ? m.User.DisplayName : string.Empty,
                m.User != null ? (m.User.NormalizedEmail ?? string.Empty) : string.Empty,
                m.User != null ? m.User.Phone : null,
                m.Role,
                m.IsActive,
                m.JoinedAt,
                m.LeftAt))
            .ToListAsync(cancellationToken);

        return Result<List<WorkspaceMemberDto>>.Success(members);
    }
}
