using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IPaymentAllocationRepository
{
    Task<List<PaymentAllocation>> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct = default);
    Task<List<PaymentAllocation>> GetActiveByChargeIdAsync(Guid chargeId, CancellationToken ct = default);
    Task AddAsync(PaymentAllocation allocation, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<PaymentAllocation> allocations, CancellationToken ct = default);
}
