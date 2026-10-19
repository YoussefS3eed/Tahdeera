using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IAuditLogRepository : IGenericRepository<AuditLog>
{
    Task<IReadOnlyList<AuditLog>> GetRecentLogsForEntitiesAsync(
        Guid workspaceId,
        IEnumerable<string> entityIds,
        int limit = 10,
        CancellationToken cancellationToken = default);
}
