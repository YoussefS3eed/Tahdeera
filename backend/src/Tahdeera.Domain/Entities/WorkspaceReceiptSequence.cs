namespace Tahdeera.Domain.Entities;

public class WorkspaceReceiptSequence
{
    public Guid WorkspaceId { get; private set; }
    public int YearMonth { get; private set; }
    public int NextSequence { get; private set; } = 1;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // Navigation
    public virtual Workspace? Workspace { get; private set; }

    private WorkspaceReceiptSequence() { } // EF Core

    public WorkspaceReceiptSequence(Guid workspaceId, int yearMonth, int nextSequence = 1)
    {
        WorkspaceId = workspaceId;
        YearMonth = yearMonth;
        NextSequence = nextSequence;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
