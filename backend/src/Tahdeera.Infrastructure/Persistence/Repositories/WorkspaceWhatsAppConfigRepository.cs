using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class WorkspaceWhatsAppConfigRepository : GenericRepository<WorkspaceWhatsAppConfig>, IWorkspaceWhatsAppConfigRepository
{
    public WorkspaceWhatsAppConfigRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<WorkspaceWhatsAppConfig?> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(x => x.ActiveSenderConnection)
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId, ct);
    }

    public new async Task AddAsync(WorkspaceWhatsAppConfig config, CancellationToken ct = default)
    {
        await base.AddAsync(config, ct);
    }
}
