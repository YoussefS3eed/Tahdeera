using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.Entities;

public class AttendanceHistory
{
    public long Id { get; set; }
    public Guid AttendanceRecordId { get; set; }
    public AttendanceStatus? OldStatus { get; set; }
    public AttendanceStatus NewStatus { get; set; }
    public AttendanceSource? OldSource { get; set; }
    public AttendanceSource NewSource { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }
    public AttendanceChangeKind ChangeKind { get; set; }
    public string Metadata { get; set; } = "{}";

    // Navigation properties
    public AttendanceRecord AttendanceRecord { get; set; } = null!;
    public AppUser? ChangedByUser { get; set; }

    public static AttendanceHistory Create(
        Guid attendanceRecordId,
        AttendanceStatus? oldStatus,
        AttendanceStatus newStatus,
        AttendanceSource? oldSource,
        AttendanceSource newSource,
        Guid? changedByUserId,
        DateTimeOffset changedAt,
        string? reason,
        AttendanceChangeKind changeKind,
        string metadata = "{}")
    {
        return new AttendanceHistory
        {
            AttendanceRecordId = attendanceRecordId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            OldSource = oldSource,
            NewSource = newSource,
            ChangedByUserId = changedByUserId,
            ChangedAt = changedAt,
            Reason = reason?.Trim(),
            ChangeKind = changeKind,
            Metadata = string.IsNullOrWhiteSpace(metadata) ? "{}" : metadata
        };
    }
}
