using Tahdeera.Domain.Common;

namespace Tahdeera.Domain.Entities;

public class WorkspaceWhatsAppUsageLedger : BaseEntity
{
    public Guid WorkspaceId { get; private set; }
    public string YearMonth { get; private set; } = string.Empty; // Format: "YYYY-MM"
    public int MessagesDispatchedCount { get; private set; }
    public int MessagesDeliveredCount { get; private set; }
    public int MessagesFailedCount { get; private set; }
    public decimal EstimatedCostUsd { get; private set; }
    public decimal EstimatedCostEgp { get; private set; }
    public string? RateCardSnapshotJson { get; private set; }

    public virtual Workspace? Workspace { get; private set; }

    private WorkspaceWhatsAppUsageLedger() { } // EF Core

    public static WorkspaceWhatsAppUsageLedger Create(
        Guid workspaceId,
        string yearMonth,
        string? rateCardSnapshotJson = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new WorkspaceWhatsAppUsageLedger
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            YearMonth = yearMonth,
            MessagesDispatchedCount = 0,
            MessagesDeliveredCount = 0,
            MessagesFailedCount = 0,
            EstimatedCostUsd = 0m,
            EstimatedCostEgp = 0m,
            RateCardSnapshotJson = rateCardSnapshotJson,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void RecordDispatch(decimal messageCostUsd, decimal messageCostEgp)
    {
        MessagesDispatchedCount++;
        EstimatedCostUsd += messageCostUsd;
        EstimatedCostEgp += messageCostEgp;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordDelivered()
    {
        MessagesDeliveredCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordFailed()
    {
        MessagesFailedCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
