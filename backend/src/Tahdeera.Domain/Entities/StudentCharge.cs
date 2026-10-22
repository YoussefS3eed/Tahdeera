using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Domain.Entities;

public class StudentCharge : BaseEntity
{
    public Guid StudentAccountId { get; private set; }
    public Guid? GroupId { get; private set; }
    public string? BillingPeriodLabel { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public ChargeStatus Status { get; private set; } = ChargeStatus.Open;
    public DateTimeOffset? CancelledAt { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? Notes { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    // Navigations
    public virtual StudentAccount? StudentAccount { get; private set; }
    public virtual Group? Group { get; private set; }
    public virtual AppUser? CancelledByUser { get; private set; }
    public virtual AppUser? CreatedByUser { get; private set; }

    private readonly List<PaymentAllocation> _allocations = new();
    public virtual IReadOnlyCollection<PaymentAllocation> Allocations => _allocations.AsReadOnly();

    private StudentCharge() { } // EF Core

    public static StudentCharge Create(
        Guid studentAccountId,
        string description,
        decimal amount,
        Guid? groupId = null,
        string? billingPeriodLabel = null,
        DateTimeOffset? dueAt = null,
        string? notes = null,
        Guid? createdByUserId = null)
    {
        if (amount <= 0)
            throw new DomainRuleViolationException("AMOUNT_MUST_BE_POSITIVE", "Amount must be strictly greater than zero.");

        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 3)
            throw new DomainRuleViolationException("CHARGE_DESCRIPTION_REQUIRED", "Charge description is required and must be at least 3 characters.");

        var now = DateTimeOffset.UtcNow;
        return new StudentCharge
        {
            Id = Guid.NewGuid(),
            StudentAccountId = studentAccountId,
            GroupId = groupId,
            BillingPeriodLabel = billingPeriodLabel?.Trim(),
            Description = description.Trim(),
            Amount = amount,
            DueAt = dueAt,
            Status = ChargeStatus.Open,
            Notes = notes?.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Cancel(string reason, Guid? userId)
    {
        if (Status != ChargeStatus.Open)
            throw new DomainRuleViolationException("CANNOT_CANCEL_NON_OPEN_CHARGE", "Cannot cancel a charge that is not in Open status.");

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
            throw new DomainRuleViolationException("CANCELLATION_REASON_TOO_SHORT", "Cancellation reason is required and must be at least 5 characters.");

        var now = DateTimeOffset.UtcNow;
        Status = ChargeStatus.Cancelled;
        CancelledAt = now;
        CancelledByUserId = userId;
        CancellationReason = reason.Trim();
        UpdatedAt = now;
    }

    public void WriteOff(string reason, Guid? userId)
    {
        if (Status != ChargeStatus.Open)
            throw new DomainRuleViolationException("CANNOT_WRITEOFF_NON_OPEN_CHARGE", "Cannot write off a charge that is not in Open status.");

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
            throw new DomainRuleViolationException("CANCELLATION_REASON_TOO_SHORT", "Write-off reason is required and must be at least 5 characters.");

        var now = DateTimeOffset.UtcNow;
        Status = ChargeStatus.WrittenOff;
        CancelledAt = now;
        CancelledByUserId = userId;
        CancellationReason = reason.Trim();
        UpdatedAt = now;
    }
}
