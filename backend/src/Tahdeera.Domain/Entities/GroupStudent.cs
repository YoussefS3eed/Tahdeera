using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.Entities;

public class GroupStudent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid StudentId { get; set; }
    public GroupStudentStatus Status { get; set; } = GroupStudentStatus.Active;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LeftAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedByUserId { get; set; }

    // Navigation properties
    public Group Group { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public AppUser? UpdatedByUser { get; set; }
    public ICollection<GroupStudentStatusHistory> StatusHistory { get; set; } = new List<GroupStudentStatusHistory>();

    public static GroupStudent Create(Guid groupId, Guid studentId, DateTimeOffset joinedAt)
    {
        return new GroupStudent
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            StudentId = studentId,
            Status = GroupStudentStatus.Active,
            JoinedAt = joinedAt,
            LeftAt = null,
            UpdatedAt = joinedAt
        };
    }

    public void Withdraw(DateTimeOffset leftAt, Guid? actorUserId)
    {
        if (leftAt < JoinedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(leftAt), "Withdrawal timestamp cannot be earlier than join timestamp.");
        }

        Status = GroupStudentStatus.Withdrawn;
        LeftAt = leftAt;
        UpdatedByUserId = actorUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Suspend(Guid? actorUserId)
    {
        Status = GroupStudentStatus.Suspended;
        UpdatedByUserId = actorUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reinstate(Guid? actorUserId)
    {
        Status = GroupStudentStatus.Active;
        LeftAt = null;
        UpdatedByUserId = actorUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
