using Tahdeera.Domain.Common;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Domain.Entities;

public class PaymentAllocation : BaseEntity
{
    public Guid PaymentId { get; private set; }
    public Guid ChargeId { get; private set; }
    public decimal Amount { get; private set; }
    public bool IsReversed { get; private set; }

    // Navigations
    public virtual StudentPayment? Payment { get; private set; }
    public virtual StudentCharge? Charge { get; private set; }

    private PaymentAllocation() { } // EF Core

    public static PaymentAllocation Create(Guid paymentId, Guid chargeId, decimal amount)
    {
        if (amount <= 0)
            throw new DomainRuleViolationException("AMOUNT_MUST_BE_POSITIVE", "Amount must be strictly greater than zero.");

        var now = DateTimeOffset.UtcNow;
        return new PaymentAllocation
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            ChargeId = chargeId,
            Amount = amount,
            IsReversed = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Reverse()
    {
        IsReversed = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
