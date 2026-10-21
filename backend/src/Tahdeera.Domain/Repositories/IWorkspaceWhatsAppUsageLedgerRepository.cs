using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IWorkspaceWhatsAppUsageLedgerRepository
{
    Task<WorkspaceWhatsAppUsageLedger?> GetOrCreateCurrentMonthAsync(Guid workspaceId, string yearMonth, CancellationToken ct = default);
    Task AddAsync(WorkspaceWhatsAppUsageLedger ledger, CancellationToken ct = default);
}
