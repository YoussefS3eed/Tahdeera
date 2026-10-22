using System.Net;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Finances.Services;

public class PaymentAllocationEngine : IPaymentAllocationEngine
{
    private readonly IStudentChargeRepository _chargeRepository;

    public PaymentAllocationEngine(IStudentChargeRepository chargeRepository)
    {
        _chargeRepository = chargeRepository;
    }

    public async Task<AllocationResult> AllocatePaymentAsync(
        StudentPayment payment,
        Guid studentAccountId,
        IReadOnlyList<PaymentAllocationRequestDto>? explicitAllocations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (explicitAllocations != null && explicitAllocations.Count > 0)
        {
            return await AllocateExplicitAsync(payment, studentAccountId, explicitAllocations, cancellationToken);
        }

        return await AllocateFifoAsync(payment, studentAccountId, cancellationToken);
    }

    private async Task<AllocationResult> AllocateExplicitAsync(
        StudentPayment payment,
        Guid studentAccountId,
        IReadOnlyList<PaymentAllocationRequestDto> explicitAllocations,
        CancellationToken cancellationToken)
    {
        var totalExplicit = explicitAllocations.Sum(a => a.Amount);
        if (totalExplicit > payment.Amount)
        {
            throw new DomainRuleViolationException(
                "ALLOCATION_EXCEEDS_PAYMENT",
                $"Total explicit allocations ({totalExplicit}) exceed the payment amount ({payment.Amount}).",
                HttpStatusCode.BadRequest);
        }

        var createdAllocations = new List<PaymentAllocation>();

        foreach (var req in explicitAllocations)
        {
            if (req.Amount <= 0)
            {
                throw new DomainRuleViolationException(
                    "AMOUNT_MUST_BE_POSITIVE",
                    "Allocation amount must be strictly greater than zero.",
                    HttpStatusCode.BadRequest);
            }

            var charge = await _chargeRepository.GetByIdAsync(req.ChargeId, cancellationToken);
            if (charge == null || charge.StudentAccountId != studentAccountId)
            {
                throw new DomainRuleViolationException(
                    "CHARGE_NOT_FOUND",
                    $"Target charge '{req.ChargeId}' does not belong to this student account.",
                    HttpStatusCode.BadRequest);
            }

            if (charge.Status != ChargeStatus.Open)
            {
                throw new DomainRuleViolationException(
                    "CHARGE_NOT_OPEN",
                    $"Target charge '{req.ChargeId}' is not open for allocation.",
                    HttpStatusCode.BadRequest);
            }

            var activeAllocated = charge.Allocations
                .Where(a => !a.IsReversed)
                .Sum(a => a.Amount);

            var remainingChargeBalance = charge.Amount - activeAllocated;

            if (req.Amount > remainingChargeBalance)
            {
                throw new DomainRuleViolationException(
                    "ALLOCATION_EXCEEDS_CHARGE_BALANCE",
                    $"Allocated amount ({req.Amount}) exceeds remaining charge balance ({remainingChargeBalance}) for charge '{charge.Id}'.",
                    HttpStatusCode.BadRequest);
            }

            var allocation = PaymentAllocation.Create(payment.Id, charge.Id, req.Amount);
            payment.AddAllocation(allocation);
            createdAllocations.Add(allocation);
        }

        var totalAllocated = createdAllocations.Sum(a => a.Amount);
        var unallocatedCredit = payment.Amount - totalAllocated;

        return new AllocationResult(totalAllocated, unallocatedCredit, createdAllocations);
    }

    private async Task<AllocationResult> AllocateFifoAsync(
        StudentPayment payment,
        Guid studentAccountId,
        CancellationToken cancellationToken)
    {
        var openCharges = await _chargeRepository.GetOpenChargesByAccountIdAsync(studentAccountId, cancellationToken);

        var createdAllocations = new List<PaymentAllocation>();
        var remainingPayment = payment.Amount;

        foreach (var charge in openCharges)
        {
            if (remainingPayment <= 0)
            {
                break;
            }

            var activeAllocated = charge.Allocations
                .Where(a => !a.IsReversed)
                .Sum(a => a.Amount);

            var unpaidBalance = charge.Amount - activeAllocated;
            if (unpaidBalance <= 0)
            {
                continue;
            }

            var allocAmount = Math.Min(remainingPayment, unpaidBalance);
            if (allocAmount > 0)
            {
                var allocation = PaymentAllocation.Create(payment.Id, charge.Id, allocAmount);
                payment.AddAllocation(allocation);
                createdAllocations.Add(allocation);
                remainingPayment -= allocAmount;
            }
        }

        var totalAllocated = payment.Amount - remainingPayment;
        var unallocatedCredit = remainingPayment;

        return new AllocationResult(totalAllocated, unallocatedCredit, createdAllocations);
    }
}
