using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class StudentAccountRepository : GenericRepository<StudentAccount>, IStudentAccountRepository
{
    public StudentAccountRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<StudentAccount?> GetByWorkspaceStudentIdAsync(Guid workspaceStudentId, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(a => a.Charges)
            .Include(a => a.Payments)
            .FirstOrDefaultAsync(a => a.WorkspaceStudentId == workspaceStudentId, ct);
    }

    public async Task<StudentAccount?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(a => a.Charges)
            .Include(a => a.Payments)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public new async Task AddAsync(StudentAccount account, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(account, ct);
    }
}
