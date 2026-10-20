using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class SessionRepository : GenericRepository<Session>, ISessionRepository
{
    public SessionRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<Session?> GetByIdWithDetailsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Group)
            .Include(s => s.AttendanceRecords)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }

    public async Task<Session?> GetByIdForWorkspaceAsync(Guid workspaceId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Group)
            .Include(s => s.AttendanceRecords)
            .FirstOrDefaultAsync(s => s.Group.OwnerWorkspaceId == workspaceId && s.Id == sessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetSessionsForGroupAsync(
        Guid groupId,
        SessionStatus? status,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(s => s.GroupId == groupId);

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.StartsAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.StartsAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(s => s.StartsAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetCompletedSessionsInPeriodAsync(
        Guid groupId,
        DateTimeOffset? since,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(s => s.GroupId == groupId && s.Status == SessionStatus.Completed);

        if (since.HasValue)
        {
            query = query.Where(s => s.StartsAt >= since.Value);
        }

        return await query
            .OrderByDescending(s => s.StartsAt)
            .ToListAsync(cancellationToken);
    }
}
