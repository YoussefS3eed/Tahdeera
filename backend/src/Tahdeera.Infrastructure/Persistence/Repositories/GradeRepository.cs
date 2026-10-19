using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class GradeRepository : GenericRepository<Grade>, IGradeRepository
{
    public GradeRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Grade>> GetActiveGradesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(g => g.IsActive)
            .OrderBy(g => g.SortOrder)
            .ToListAsync(cancellationToken);
    }
}
