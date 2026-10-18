namespace Tahdeera.Domain.Entities;

public class AcademicYear
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public bool IsCurrent { get; set; } = false;

    // Navigation properties
    public ICollection<StudentAcademicHistory> AcademicHistories { get; set; } = new List<StudentAcademicHistory>();
}
