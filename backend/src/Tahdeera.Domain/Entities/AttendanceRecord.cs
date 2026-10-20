using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.Entities;

public class AttendanceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid StudentId { get; set; }
    public Guid OwnerWorkspaceId { get; set; }
    public AttendanceStatus Status { get; set; }
    public AttendanceSource Source { get; set; }
    public bool AutoEvaluationLocked { get; set; } = false;
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CheckInAt { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTimeOffset LastModifiedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? LastModifiedByUserId { get; set; }
    public string? RetroactiveReason { get; set; }
    public DateTimeOffset? SystemGeneratedAt { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }
    public string? VoidReason { get; set; }
    public bool IsGuestMakeup { get; set; } = false;
    public Guid? HomeGroupId { get; set; }

    // Navigation properties
    public Session Session { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public Workspace OwnerWorkspace { get; set; } = null!;
    public Group? HomeGroup { get; set; }
    public AppUser? RecordedByUser { get; set; }
    public AppUser? LastModifiedByUser { get; set; }
    public ICollection<AttendanceHistory> HistoryEntries { get; set; } = new List<AttendanceHistory>();
    public ICollection<MessageOutbox> OutboxMessages { get; set; } = new List<MessageOutbox>();

    public static AttendanceRecord CreateCheckIn(
        Session session,
        Guid studentId,
        Guid workspaceId,
        DateTimeOffset checkInAt,
        Guid? userId)
    {
        var punctualityStatus = session.EvaluatePunctuality(checkInAt);
        return new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            StudentId = studentId,
            OwnerWorkspaceId = workspaceId,
            Status = punctualityStatus,
            Source = userId.HasValue ? AttendanceSource.User : AttendanceSource.System,
            CheckInAt = checkInAt,
            RecordedByUserId = userId,
            RecordedAt = checkInAt,
            LastModifiedAt = checkInAt,
            AutoEvaluationLocked = false,
            IsGuestMakeup = false,
            HomeGroupId = null
        };
    }

    public static AttendanceRecord CreateSystemAbsence(
        Guid sessionId,
        Guid studentId,
        Guid workspaceId,
        DateTimeOffset systemNow)
    {
        return new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            StudentId = studentId,
            OwnerWorkspaceId = workspaceId,
            Status = AttendanceStatus.Absent,
            Source = AttendanceSource.System,
            RecordedByUserId = null,
            RecordedAt = systemNow,
            SystemGeneratedAt = systemNow,
            LastModifiedAt = systemNow,
            AutoEvaluationLocked = false,
            IsGuestMakeup = false,
            HomeGroupId = null
        };
    }

    public static AttendanceRecord RegisterMakeup(
        Session session,
        Guid studentId,
        Guid workspaceId,
        Guid homeGroupId,
        DateTimeOffset checkInAt,
        Guid? userId)
    {
        return RegisterMakeup(session, studentId, workspaceId, homeGroupId, null, checkInAt, userId);
    }

    public static AttendanceRecord RegisterMakeup(
        Session session,
        Guid studentId,
        Guid workspaceId,
        Guid homeGroupId,
        string? reason,
        DateTimeOffset checkInAt,
        Guid? userId)
    {
        var punctualityStatus = session.EvaluatePunctuality(checkInAt);
        return new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            StudentId = studentId,
            OwnerWorkspaceId = workspaceId,
            Status = punctualityStatus,
            Source = userId.HasValue ? AttendanceSource.User : AttendanceSource.System,
            CheckInAt = checkInAt,
            RecordedByUserId = userId,
            RecordedAt = checkInAt,
            LastModifiedAt = checkInAt,
            AutoEvaluationLocked = false,
            IsGuestMakeup = true,
            HomeGroupId = homeGroupId,
            RetroactiveReason = reason
        };
    }

    public void ManualOverride(
        AttendanceStatus newStatus,
        Guid actorUserId,
        DateTimeOffset modifiedAt,
        string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = newStatus;
        AutoEvaluationLocked = true;
        LastModifiedByUserId = actorUserId;
        LastModifiedAt = modifiedAt;
        RetroactiveReason = reason.Trim();
    }

    public void ReevaluatePunctuality(Session session)
    {
        if (AutoEvaluationLocked || !CheckInAt.HasValue || VoidedAt != null)
        {
            return;
        }

        var newPunctuality = session.EvaluatePunctuality(CheckInAt.Value);
        if (Status != newPunctuality)
        {
            Status = newPunctuality;
            LastModifiedAt = DateTimeOffset.UtcNow;
        }
    }

    public void Void(string cancellationReason, DateTimeOffset voidedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cancellationReason);

        VoidedAt = voidedAt;
        VoidReason = cancellationReason.Trim();
        LastModifiedAt = voidedAt;
    }
}
