using Tahdeera.Domain.Common;

namespace Tahdeera.Domain.Entities;

public class WorkspaceSubscription : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public Guid? PlanId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string Status { get; set; } = "Active"; // 'Active', 'Expired', 'Cancelled'
    public DateTimeOffset? ManualSuspendedAt { get; set; }
    public Guid? ManualSuspendedByUserId { get; set; }
    public string? SuspensionReason { get; set; }
    public decimal AgreedPrice { get; set; }
    public int? MaxStudentsOverride { get; set; }
    public int? MaxStaffOverride { get; set; }
    public string FeatureOverrides { get; set; } = "{}";
    public Guid? CreatedByUserId { get; set; }

    // Navigation properties
    public Workspace? Workspace { get; set; }
    public SubscriptionPlan? Plan { get; set; }
    public AppUser? ManualSuspendedByUser { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public ICollection<SubscriptionPayment> Payments { get; set; } = new List<SubscriptionPayment>();

    public bool IsLockedOut(DateTimeOffset now)
    {
        return ManualSuspendedAt != null
            || Status != "Active"
            || EndsAt < now;
    }

    public void Suspend(Guid adminId, string reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("SUSPENSION_REASON_REQUIRED", "Suspension reason cannot be empty.");

        ManualSuspendedAt = now;
        ManualSuspendedByUserId = adminId;
        SuspensionReason = reason.Trim();
        UpdatedAt = now;
    }

    public void Reactivate(DateTimeOffset now)
    {
        ManualSuspendedAt = null;
        ManualSuspendedByUserId = null;
        SuspensionReason = null;
        if (EndsAt > now)
        {
            Status = "Active";
        }
        UpdatedAt = now;
    }

    public void Renew(int durationDays, decimal price, DateTimeOffset now, DateTimeOffset? overrideStarts = null, DateTimeOffset? overrideEnds = null)
    {
        if (durationDays <= 0)
            throw new DomainException("INVALID_DURATION", "Renewal duration must be greater than zero days.");

        if (price < 0)
            throw new DomainException("INVALID_PRICE", "Agreed price cannot be negative.");

        DateTimeOffset effectiveStart = overrideStarts ?? (EndsAt > now ? StartsAt : now);
        DateTimeOffset baseForEnd = (EndsAt > now) ? EndsAt : now;
        DateTimeOffset effectiveEnd = overrideEnds ?? baseForEnd.AddDays(durationDays);

        if (effectiveEnd <= effectiveStart)
            throw new DomainException("INVALID_DATE_RANGE", "Subscription ends date must be strictly after starts date.");

        StartsAt = effectiveStart;
        EndsAt = effectiveEnd;
        AgreedPrice = price;
        Status = "Active";
        UpdatedAt = now;
    }
}
