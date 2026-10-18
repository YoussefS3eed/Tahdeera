namespace Tahdeera.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? IpAddress { get; set; }
    public string Metadata { get; set; } = "{}";

    // Navigation properties
    public AppUser? ActorUser { get; set; }
    public Workspace? Workspace { get; set; }
}
