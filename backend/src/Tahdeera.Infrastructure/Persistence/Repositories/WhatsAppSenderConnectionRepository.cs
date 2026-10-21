using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class WhatsAppSenderConnectionRepository : GenericRepository<WhatsAppSenderConnection>, IWhatsAppSenderConnectionRepository
{
    public WhatsAppSenderConnectionRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<WhatsAppSenderConnection?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<WhatsAppSenderConnection?> GetActiveForWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.Status == SenderConnectionStatus.Connected, ct);
    }

    public async Task<IReadOnlyList<WhatsAppSenderConnection>> GetHistoryForWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public new async Task AddAsync(WhatsAppSenderConnection connection, CancellationToken ct = default)
    {
        await base.AddAsync(connection, ct);
    }
}
