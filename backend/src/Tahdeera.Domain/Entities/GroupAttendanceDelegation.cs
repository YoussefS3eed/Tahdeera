namespace Tahdeera.Domain.Entities;

public class GroupAttendanceDelegation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid AssistantUserId { get; set; }
    public Guid GrantedByTeacherUserId { get; set; }
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? RevokedByUserId { get; set; }
    public string? RevocationReason { get; set; }

    // Navigation properties
    public Group Group { get; set; } = null!;
    public AppUser AssistantUser { get; set; } = null!;
    public AppUser GrantedByTeacherUser { get; set; } = null!;
    public AppUser? RevokedByUser { get; set; }

    public static GroupAttendanceDelegation Create(
        Guid groupId,
        Guid assistantUserId,
        Guid grantedByTeacherUserId)
    {
        return new GroupAttendanceDelegation
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            AssistantUserId = assistantUserId,
            GrantedByTeacherUserId = grantedByTeacherUserId,
            GrantedAt = DateTimeOffset.UtcNow,
            RevokedAt = null
        };
    }

    public void Revoke(Guid revokedByUserId, string? reason, DateTimeOffset revokedAt)
    {
        if (RevokedAt != null)
        {
            throw new InvalidOperationException("Attendance delegation is already revoked.");
        }

        if (revokedAt < GrantedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(revokedAt), "Revocation timestamp cannot be earlier than grant timestamp.");
        }

        RevokedAt = revokedAt;
        RevokedByUserId = revokedByUserId;
        RevocationReason = reason?.Trim();
    }
}
