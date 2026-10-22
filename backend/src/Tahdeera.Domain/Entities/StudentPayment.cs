using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Domain.Entities;

public class StudentPayment : BaseEntity
{
    public Guid StudentAccountId { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTimeOffset PaidAt { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }
    public Guid RecordedByUserId { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Received;
    public DateTimeOffset? ReversedAt { get; private set; }
    public Guid? ReversedByUserId { get; private set; }
    public string? ReversalReason { get; private set; }
    public Guid? ReversesPaymentId { get; private set; }

    // Navigations
    public virtual StudentAccount? StudentAccount { get; private set; }
    public virtual AppUser? RecordedByUser { get; private set; }
    public virtual AppUser? ReversedByUser { get; private set; }
    public virtual StudentPayment? ReversesPayment { get; private set; }

    private readonly List<PaymentAllocation> _allocations = new();
    public virtual IReadOnlyCollection<PaymentAllocation> Allocations => _allocations.AsReadOnly();

    private StudentPayment() { } // EF Core

    public static StudentPayment Create(
        Guid studentAccountId,
        string receiptNumber,
        decimal amount,
        PaymentMethod method,
        Guid recordedByUserId,
        DateTimeOffset? paidAt = null,
        string? reference = null,
        string? notes = null,
        Guid? reversesPaymentId = null)
    {
        if (amount <= 0)
            throw new DomainRuleViolationException("AMOUNT_MUST_BE_POSITIVE", "Amount must be strictly greater than zero.");

        var paymentDate = paidAt ?? DateTimeOffset.UtcNow;
        if (paymentDate > DateTimeOffset.UtcNow.AddMinutes(5))
            throw new DomainRuleViolationException("PAYMENT_DATE_CANNOT_BE_FUTURE", "Payment date cannot be in the future.");

        if (string.IsNullOrWhiteSpace(receiptNumber))
            throw new DomainRuleViolationException("PAYMENT_NOT_FOUND", "Receipt number is required.");

        var now = DateTimeOffset.UtcNow;
        return new StudentPayment
        {
            Id = Guid.NewGuid(),
            StudentAccountId = studentAccountId,
            ReceiptNumber = receiptNumber.Trim(),
            Amount = amount,
            PaidAt = paymentDate,
            Method = method,
            RecordedByUserId = recordedByUserId,
            Reference = reference?.Trim(),
            Notes = notes?.Trim(),
            Status = PaymentStatus.Received,
            ReversesPaymentId = reversesPaymentId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void AddAllocation(PaymentAllocation allocation)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        _allocations.Add(allocation);
    }

    public void Reverse(string reason, Guid? userId)
    {
        if (Status != PaymentStatus.Received)
            throw new DomainRuleViolationException("PAYMENT_ALREADY_REVERSED", "This payment has already been reversed.");

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
            throw new DomainRuleViolationException("REVERSAL_REASON_TOO_SHORT", "Payment reversal reason is required and must be at least 5 characters.");

        var now = DateTimeOffset.UtcNow;
        Status = PaymentStatus.Reversed;
        ReversedAt = now;
        ReversedByUserId = userId;
        ReversalReason = reason.Trim();
        UpdatedAt = now;

        foreach (var allocation in _allocations)
        {
            allocation.Reverse();
        }
    }
}
