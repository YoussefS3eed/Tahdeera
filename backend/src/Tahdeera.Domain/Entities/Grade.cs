namespace Tahdeera.Domain.Entities;

public class Grade
{
    public short Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<StudentAcademicHistory> AcademicHistories { get; set; } = new List<StudentAcademicHistory>();
}
