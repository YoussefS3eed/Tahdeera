using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface ISubscriptionPlanRepository : IGenericRepository<SubscriptionPlan>
{
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<SubscriptionPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<List<SubscriptionPlan>> GetPlansAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
}
