using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.Entities;

public class WorkspaceStudent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public Guid StudentId { get; set; }
    public WorkspaceStudentStatus Status { get; set; } = WorkspaceStudentStatus.Active;
    public string? StudentPhone { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public bool WhatsappOptIn { get; set; } = false;
    public DateTimeOffset? WhatsappOptInAt { get; set; }
    public string? SeatNumber { get; set; }
    public string? LocalNotes { get; set; }
    public DateTimeOffset EnrolledAt { get; init; } = DateTimeOffset.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Workspace Workspace { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public AppUser? CreatedByUser { get; set; }
    public virtual StudentAccount? StudentAccount { get; set; }

    public void SetStatus(WorkspaceStudentStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateContactInfo(
        string? studentPhone,
        string? guardianName,
        string? guardianPhone,
        bool whatsappOptIn,
        string? seatNumber,
        string? localNotes)
    {
        StudentPhone = studentPhone?.Trim();
        GuardianName = guardianName?.Trim();
        GuardianPhone = guardianPhone?.Trim();
        SeatNumber = seatNumber?.Trim();
        LocalNotes = localNotes?.Trim();

        if (whatsappOptIn && !WhatsappOptIn)
        {
            WhatsappOptInAt = DateTimeOffset.UtcNow;
        }
        else if (!whatsappOptIn)
        {
            WhatsappOptInAt = null;
        }

        WhatsappOptIn = whatsappOptIn;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
