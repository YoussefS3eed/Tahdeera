using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class SubscriptionPlanRepository : GenericRepository<SubscriptionPlan>, ISubscriptionPlanRepository
{
    public SubscriptionPlanRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var codeNormalized = code.Trim().ToUpperInvariant();
        return await _dbSet.AnyAsync(p => p.Code == codeNormalized, cancellationToken);
    }

    public async Task<SubscriptionPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var codeNormalized = code.Trim().ToUpperInvariant();
        return await _dbSet.FirstOrDefaultAsync(p => p.Code == codeNormalized, cancellationToken);
    }

    public async Task<List<SubscriptionPlan>> GetPlansAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.DefaultPrice)
            .ToListAsync(cancellationToken);
    }
}
