namespace Tahdeera.Application.Features.Auth.DTOs;

public record UserProfileDto(
    Guid Id,
    string Username,
    string? Email,
    string DisplayName,
    string? Phone,
    string PreferredLanguage,
    string PreferredTheme,
    bool IsPlatformAdmin);
