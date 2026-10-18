namespace Tahdeera.Domain.Entities;

public class StudentAcademicHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Guid AcademicYearId { get; set; }
    public short GradeId { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? RecordedByUserId { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Student Student { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
    public Grade Grade { get; set; } = null!;
    public AppUser? RecordedByUser { get; set; }
}
