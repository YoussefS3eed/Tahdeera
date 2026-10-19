using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.Entities;

public class Group
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerWorkspaceId { get; set; }
    public Guid AcademicYearId { get; set; }
    public short GradeId { get; set; }
    public Guid? SubjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RoomName { get; set; }
    public string? ScheduleSummary { get; set; }
    public int LateGraceMinutes { get; set; } = 20;
    public decimal DefaultMonthlyFee { get; set; } = 0.00m;
    public short? ConsecutiveAbsenceThreshold { get; set; }
    public AbsenceAction AbsenceAction { get; set; } = AbsenceAction.NotifyOnly;
    public GroupStatus Status { get; set; } = GroupStatus.Active;
    public Guid ResponsibleTeacherUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Workspace OwnerWorkspace { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
    public Grade Grade { get; set; } = null!;
    public Subject? Subject { get; set; }
    public AppUser ResponsibleTeacherUser { get; set; } = null!;
    public AppUser? CreatedByUser { get; set; }
    public ICollection<GroupStudent> EnrolledStudents { get; set; } = new List<GroupStudent>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<GroupWorkspaceAccess> WorkspaceAccesses { get; set; } = new List<GroupWorkspaceAccess>();
    public ICollection<GroupAttendanceDelegation> Delegations { get; set; } = new List<GroupAttendanceDelegation>();

    public static Group Create(
        Guid ownerWorkspaceId,
        Guid academicYearId,
        short gradeId,
        string name,
        Guid responsibleTeacherUserId,
        Guid? subjectId = null,
        string? roomName = null,
        string? scheduleSummary = null,
        int lateGraceMinutes = 20,
        decimal defaultMonthlyFee = 0.00m,
        short? consecutiveAbsenceThreshold = null,
        AbsenceAction absenceAction = AbsenceAction.NotifyOnly,
        Guid? createdByUserId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (lateGraceMinutes < 0 || lateGraceMinutes > 1440)
        {
            throw new ArgumentOutOfRangeException(nameof(lateGraceMinutes), "Late grace minutes must be between 0 and 1440.");
        }

        if (defaultMonthlyFee < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultMonthlyFee), "Default monthly fee cannot be negative.");
        }

        if (consecutiveAbsenceThreshold.HasValue && (consecutiveAbsenceThreshold.Value < 1 || consecutiveAbsenceThreshold.Value > 50))
        {
            throw new ArgumentOutOfRangeException(nameof(consecutiveAbsenceThreshold), "Consecutive absence threshold must be between 1 and 50.");
        }

        var now = DateTimeOffset.UtcNow;
        return new Group
        {
            Id = Guid.NewGuid(),
            OwnerWorkspaceId = ownerWorkspaceId,
            AcademicYearId = academicYearId,
            GradeId = gradeId,
            SubjectId = subjectId,
            Name = name.Trim(),
            RoomName = roomName?.Trim(),
            ScheduleSummary = scheduleSummary?.Trim(),
            LateGraceMinutes = lateGraceMinutes,
            DefaultMonthlyFee = defaultMonthlyFee,
            ConsecutiveAbsenceThreshold = consecutiveAbsenceThreshold,
            AbsenceAction = absenceAction,
            Status = GroupStatus.Active,
            ResponsibleTeacherUserId = responsibleTeacherUserId,
            CreatedAt = now,
            CreatedByUserId = createdByUserId,
            UpdatedAt = now
        };
    }

    public void UpdateDetails(
        string name,
        string? roomName,
        string? scheduleSummary,
        Guid? subjectId,
        Guid? modifiedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        RoomName = roomName?.Trim();
        ScheduleSummary = scheduleSummary?.Trim();
        SubjectId = subjectId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateSchedule(int lateGraceMinutes, Guid? modifiedByUserId)
    {
        if (lateGraceMinutes < 0 || lateGraceMinutes > 1440)
        {
            throw new ArgumentOutOfRangeException(nameof(lateGraceMinutes), "Late grace minutes must be between 0 and 1440.");
        }

        LateGraceMinutes = lateGraceMinutes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateAbsencePolicy(
        short? consecutiveAbsenceThreshold,
        AbsenceAction absenceAction,
        Guid? modifiedByUserId)
    {
        if (consecutiveAbsenceThreshold.HasValue && (consecutiveAbsenceThreshold.Value < 1 || consecutiveAbsenceThreshold.Value > 50))
        {
            throw new ArgumentOutOfRangeException(nameof(consecutiveAbsenceThreshold), "Consecutive absence threshold must be between 1 and 50.");
        }

        ConsecutiveAbsenceThreshold = consecutiveAbsenceThreshold;
        AbsenceAction = absenceAction;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetStatus(GroupStatus newStatus, Guid? modifiedByUserId)
    {
        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
