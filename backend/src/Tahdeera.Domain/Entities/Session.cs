using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.Entities;

public class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public int LateGraceMinutes { get; set; } = 20;
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    public string? CancellationReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public DateTimeOffset? AutoAbsenceProcessedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Group Group { get; set; } = null!;
    public AppUser? CreatedByUser { get; set; }
    public AppUser? CancelledByUser { get; set; }
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    public static Session Create(
        Guid groupId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        int lateGraceMinutes = 20,
        Guid? createdByUserId = null)
    {
        if (endsAt <= startsAt)
        {
            throw new ArgumentException("Session end time must be strictly after start time.", nameof(endsAt));
        }

        if (lateGraceMinutes < 0 || lateGraceMinutes > 1440)
        {
            throw new ArgumentOutOfRangeException(nameof(lateGraceMinutes), "Late grace minutes must be between 0 and 1440.");
        }

        var now = DateTimeOffset.UtcNow;
        return new Session
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            LateGraceMinutes = lateGraceMinutes,
            Status = SessionStatus.Scheduled,
            CreatedAt = now,
            CreatedByUserId = createdByUserId,
            UpdatedAt = now
        };
    }

    public void UpdateSchedule(
        DateTimeOffset newStartsAt,
        DateTimeOffset newEndsAt,
        int newGraceMinutes,
        Guid? modifiedByUserId)
    {
        if (Status != SessionStatus.Scheduled)
        {
            throw new InvalidOperationException("Schedule cannot be modified for a completed or cancelled session.");
        }

        if (newEndsAt <= newStartsAt)
        {
            throw new ArgumentException("Session end time must be strictly after start time.", nameof(newEndsAt));
        }

        if (newGraceMinutes < 0 || newGraceMinutes > 1440)
        {
            throw new ArgumentOutOfRangeException(nameof(newGraceMinutes), "Late grace minutes must be between 0 and 1440.");
        }

        StartsAt = newStartsAt;
        EndsAt = newEndsAt;
        LateGraceMinutes = newGraceMinutes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(string reason, Guid cancelledByUserId, DateTimeOffset cancelledAt)
    {
        if (Status == SessionStatus.Cancelled)
        {
            throw new InvalidOperationException("Session is already cancelled.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = SessionStatus.Cancelled;
        CancellationReason = reason.Trim();
        CancelledAt = cancelledAt;
        CancelledByUserId = cancelledByUserId;
        UpdatedAt = cancelledAt;
    }

    public void MarkCompleted(DateTimeOffset autoAbsenceProcessedAt)
    {
        Status = SessionStatus.Completed;
        AutoAbsenceProcessedAt = autoAbsenceProcessedAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public AttendanceStatus EvaluatePunctuality(DateTimeOffset checkInAt)
    {
        var graceDeadline = StartsAt.AddMinutes(LateGraceMinutes);
        return checkInAt <= graceDeadline ? AttendanceStatus.Present : AttendanceStatus.Late;
    }
}
