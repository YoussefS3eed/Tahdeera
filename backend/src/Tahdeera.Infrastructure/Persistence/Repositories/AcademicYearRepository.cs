using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class AcademicYearRepository : GenericRepository<AcademicYear>, IAcademicYearRepository
{
    public AcademicYearRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<AcademicYear?> GetCurrentYearAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(y => y.IsCurrent, cancellationToken);
    }

    public async Task<IReadOnlyList<AcademicYear>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .OrderByDescending(y => y.StartsOn)
            .ToListAsync(cancellationToken);
    }
}
