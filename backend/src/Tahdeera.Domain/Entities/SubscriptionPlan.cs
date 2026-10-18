using Tahdeera.Domain.Common;
using Tahdeera.Domain.ValueObjects;

namespace Tahdeera.Domain.Entities;

public class SubscriptionPlan : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public decimal DefaultPrice { get; set; }
    public string Currency { get; set; } = "EGP";
    public int DurationDays { get; set; } = 365;
    public int MaxStudents { get; set; } = 150;
    public int MaxStaff { get; set; } = 1;
    public int MaxBranches { get; set; } = 1;
    public SubscriptionPlanFeatures Features { get; set; } = new();
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<WorkspaceSubscription> Subscriptions { get; set; } = new List<WorkspaceSubscription>();
}
