using Tahdeera.Domain.Common;

namespace Tahdeera.Domain.Entities;

public class WorkspaceMember : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Teacher";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LeftAt { get; set; }
    public Guid? CreatedByUserId { get; set; }

    // Navigation properties
    public Workspace? Workspace { get; set; }
    public AppUser? User { get; set; }
    public AppUser? CreatedByUser { get; set; }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        LeftAt = now;
        UpdatedAt = now;
    }

    public void Reactivate()
    {
        IsActive = true;
        LeftAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
