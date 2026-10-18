using MediatR;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Workspaces.Queries.GetAdminWorkspaces;

public class GetAdminWorkspacesQueryHandler : IRequestHandler<GetAdminWorkspacesQuery, Result<PaginatedList<AdminWorkspaceListItemDto>>>
{
    private readonly ITahdeeraDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAdminWorkspacesQueryHandler(ITahdeeraDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PaginatedList<AdminWorkspaceListItemDto>>> Handle(GetAdminWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => request.PageSize
        };

        var query = _context.Workspaces
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(search)
                || _context.WorkspaceMembers
                    .Where(m => m.WorkspaceId == w.Id && m.Role == "Owner" && m.IsActive)
                    .Select(m => m.User!.NormalizedEmail)
                    .Any(email => email != null && email.ToLower().Contains(search)));
        }

        var now = _dateTimeProvider.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            if (status.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(w => _context.WorkspaceSubscriptions
                    .Where(s => s.WorkspaceId == w.Id)
                    .OrderByDescending(s => s.EndsAt)
                    .Select(s => s.ManualSuspendedAt)
                    .FirstOrDefault() != null);
            }
            else if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(w => _context.WorkspaceSubscriptions
                    .Where(s => s.WorkspaceId == w.Id)
                    .OrderByDescending(s => s.EndsAt)
                    .Any(s => s.Status == "Active" && s.ManualSuspendedAt == null && s.EndsAt >= now));
            }
            else if (status.Equals("Expired", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(w => _context.WorkspaceSubscriptions
                    .Where(s => s.WorkspaceId == w.Id)
                    .OrderByDescending(s => s.EndsAt)
                    .Any(s => s.EndsAt < now || s.Status == "Expired"));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var workspaces = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new
            {
                w.Id,
                w.Name,
                w.Kind,
                w.CreatedAt,
                Owner = _context.WorkspaceMembers
                    .Where(m => m.WorkspaceId == w.Id && m.Role == "Owner" && m.IsActive)
                    .Select(m => new { m.User!.DisplayName, m.User!.NormalizedEmail })
                    .FirstOrDefault(),
                LatestSubscription = _context.WorkspaceSubscriptions
                    .Where(s => s.WorkspaceId == w.Id)
                    .OrderByDescending(s => s.EndsAt)
                    .Select(s => new
                    {
                        s.Status,
                        s.EndsAt,
                        IsSuspended = s.ManualSuspendedAt != null
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var items = workspaces.Select(w => new AdminWorkspaceListItemDto(
            w.Id,
            w.Name,
            w.Kind,
            w.Owner?.DisplayName,
            w.Owner?.NormalizedEmail?.ToLowerInvariant(),
            w.LatestSubscription?.Status ?? "None",
            w.LatestSubscription?.EndsAt,
            w.LatestSubscription?.IsSuspended ?? false,
            w.CreatedAt
        )).ToList();

        return Result.Success(new PaginatedList<AdminWorkspaceListItemDto>(items, totalCount, page, pageSize));
    }
}
