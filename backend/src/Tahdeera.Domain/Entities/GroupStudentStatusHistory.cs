using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.Entities;

public class GroupStudentStatusHistory
{
    public long Id { get; set; }
    public Guid GroupStudentId { get; set; }
    public GroupStudentStatus? OldStatus { get; set; }
    public GroupStudentStatus NewStatus { get; set; }
    public DateTimeOffset EffectiveAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ChangedByUserId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public GroupStudent GroupStudent { get; set; } = null!;
    public AppUser? ChangedByUser { get; set; }

    public static GroupStudentStatusHistory Record(
        Guid groupStudentId,
        GroupStudentStatus? oldStatus,
        GroupStudentStatus newStatus,
        DateTimeOffset effectiveAt,
        Guid? changedByUserId,
        string? reason)
    {
        return new GroupStudentStatusHistory
        {
            GroupStudentId = groupStudentId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            EffectiveAt = effectiveAt,
            ChangedByUserId = changedByUserId,
            Reason = reason?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
