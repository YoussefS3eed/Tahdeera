using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class StudentChargeRepository : GenericRepository<StudentCharge>, IStudentChargeRepository
{
    public StudentChargeRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<StudentCharge?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(c => c.Allocations)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<StudentCharge>> GetOpenChargesByAccountIdAsync(Guid studentAccountId, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(c => c.Allocations)
            .Where(c => c.StudentAccountId == studentAccountId && c.Status == ChargeStatus.Open)
            .OrderBy(c => c.DueAt ?? c.CreatedAt)
            .ThenBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToListAsync(ct);
    }

    public new async Task AddAsync(StudentCharge charge, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(charge, ct);
    }
}
