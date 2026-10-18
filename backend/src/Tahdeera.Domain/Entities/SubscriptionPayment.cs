using Tahdeera.Domain.Common;

namespace Tahdeera.Domain.Entities;

public class SubscriptionPayment : BaseEntity
{
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset PaidAt { get; set; } = DateTimeOffset.UtcNow;
    public string Method { get; set; } = "Cash"; // 'Cash', 'BankTransfer', 'Wallet', 'Card', 'Other'
    public string? Reference { get; set; }
    public Guid RecordedByUserId { get; set; }
    public string Status { get; set; } = "Received"; // 'Received', 'Reversed'
    public string? ReversalReason { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public WorkspaceSubscription? Subscription { get; set; }
    public AppUser? RecordedByUser { get; set; }

    public void Reverse(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("REVERSAL_REASON_REQUIRED", "Reversal reason cannot be empty.");

        Status = "Reversed";
        ReversalReason = reason.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
