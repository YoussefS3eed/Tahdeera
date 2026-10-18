using Tahdeera.Domain.Common;

namespace Tahdeera.Domain.Entities;

public class AppUser : BaseEntity
{
    public string? IdentitySubject { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? NormalizedEmail { get; set; }
    public string? Phone { get; set; }
    public string PreferredLanguage { get; set; } = "ar";
    public string PreferredTheme { get; set; } = "light";
    public bool IsPlatformAdmin { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<WorkspaceMember> WorkspaceMemberships { get; set; } = new List<WorkspaceMember>();

    public void UpdatePreferences(string lang, string theme)
    {
        if (lang != "ar" && lang != "en")
            throw new DomainException("INVALID_LANGUAGE", "Preferred language must be either 'ar' or 'en'.");

        if (theme != "light" && theme != "dark")
            throw new DomainException("INVALID_THEME", "Preferred theme must be 'light' or 'dark'.");

        PreferredLanguage = lang;
        PreferredTheme = theme;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
        UpdatedAt = now;
    }
}
