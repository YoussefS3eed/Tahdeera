using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class PaymentAllocationRepository : GenericRepository<PaymentAllocation>, IPaymentAllocationRepository
{
    public PaymentAllocationRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<List<PaymentAllocation>> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(a => a.PaymentId == paymentId)
            .ToListAsync(ct);
    }

    public async Task<List<PaymentAllocation>> GetActiveByChargeIdAsync(Guid chargeId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(a => a.ChargeId == chargeId && !a.IsReversed)
            .ToListAsync(ct);
    }

    public new async Task AddAsync(PaymentAllocation allocation, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(allocation, ct);
    }

    public override async Task AddRangeAsync(IEnumerable<PaymentAllocation> allocations, CancellationToken ct = default)
    {
        await _dbSet.AddRangeAsync(allocations, ct);
    }
}
