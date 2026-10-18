using Tahdeera.Domain.Common;

namespace Tahdeera.Domain.Entities;

public class Workspace : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "Teacher"; // 'Teacher' or 'Center'
    public string TimezoneId { get; set; } = "Africa/Cairo";
    public int DefaultLateGraceMinutes { get; set; } = 20;
    public int[] WeeklyHolidayDays { get; set; } = [5]; // 5 = Friday in ISO/JS
    public string? ContactPhone { get; set; }
    public string? CityAddress { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedByUserId { get; set; }

    // Navigation properties
    public AppUser? CreatedByUser { get; set; }
    public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    public ICollection<WorkspaceSubscription> Subscriptions { get; set; } = new List<WorkspaceSubscription>();
}
