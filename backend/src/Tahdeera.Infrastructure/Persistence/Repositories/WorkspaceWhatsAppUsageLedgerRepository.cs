using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class WorkspaceWhatsAppUsageLedgerRepository : GenericRepository<WorkspaceWhatsAppUsageLedger>, IWorkspaceWhatsAppUsageLedgerRepository
{
    public WorkspaceWhatsAppUsageLedgerRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<WorkspaceWhatsAppUsageLedger?> GetOrCreateCurrentMonthAsync(Guid workspaceId, string yearMonth, CancellationToken ct = default)
    {
        var ledger = await _dbSet.FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.YearMonth == yearMonth, ct);
        if (ledger == null)
        {
            ledger = WorkspaceWhatsAppUsageLedger.Create(workspaceId, yearMonth);
            await _dbSet.AddAsync(ledger, ct);
        }
        return ledger;
    }

    public new async Task AddAsync(WorkspaceWhatsAppUsageLedger ledger, CancellationToken ct = default)
    {
        await base.AddAsync(ledger, ct);
    }
}
