using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class AuditLogRepository : GenericRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentLogsForEntitiesAsync(
        Guid workspaceId,
        IEnumerable<string> entityIds,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var idList = entityIds.ToList();
        return await _dbSet.AsNoTracking()
            .Include(a => a.ActorUser)
            .Where(a => a.WorkspaceId == workspaceId && idList.Contains(a.EntityId!))
            .OrderByDescending(a => a.OccurredAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
