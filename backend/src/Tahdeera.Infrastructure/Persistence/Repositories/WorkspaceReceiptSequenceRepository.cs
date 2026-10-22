using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class WorkspaceReceiptSequenceRepository : IWorkspaceReceiptSequenceRepository
{
    private readonly TahdeeraDbContext _context;

    public WorkspaceReceiptSequenceRepository(TahdeeraDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetNextSequenceAsync(Guid workspaceId, int yearMonth, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO workspace_receipt_sequences (workspace_id, year_month, next_sequence, updated_at)
            VALUES ({0}, {1}, 1, now())
            ON CONFLICT (workspace_id, year_month)
            DO UPDATE SET next_sequence = workspace_receipt_sequences.next_sequence + 1, updated_at = now()
            RETURNING next_sequence;
            """;

        var result = await _context.Database
            .SqlQueryRaw<int>(sql, workspaceId, yearMonth)
            .ToListAsync(ct);

        return result.Single();
    }
}
