using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Application.Features.Finances.Services;

public interface IPaymentAllocationEngine
{
    Task<AllocationResult> AllocatePaymentAsync(
        StudentPayment payment,
        Guid studentAccountId,
        IReadOnlyList<PaymentAllocationRequestDto>? explicitAllocations,
        CancellationToken cancellationToken);
}

public record AllocationResult(decimal TotalAllocated, decimal UnallocatedCredit, IReadOnlyList<PaymentAllocation> Allocations);
