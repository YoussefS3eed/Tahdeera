namespace Tahdeera.Domain.Entities;

public class Student
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StudentCode { get; set; } = string.Empty;
    public byte[]? VerificationPinHash { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public ICollection<WorkspaceStudent> WorkspaceEnrollments { get; set; } = new List<WorkspaceStudent>();
    public ICollection<StudentAcademicHistory> AcademicHistories { get; set; } = new List<StudentAcademicHistory>();
    public ICollection<StudentCredential> Credentials { get; set; } = new List<StudentCredential>();
    public ICollection<GroupStudent> GroupEnrollments { get; set; } = new List<GroupStudent>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<MessageOutbox> OutboxMessages { get; set; } = new List<MessageOutbox>();

    public void UpdateName(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        var trimmed = newName.Trim();
        if (trimmed.Length > 200)
        {
            throw new ArgumentException("Full name cannot exceed 200 characters.", nameof(newName));
        }

        FullName = trimmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
