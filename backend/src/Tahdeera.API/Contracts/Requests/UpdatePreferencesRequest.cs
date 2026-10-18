namespace Tahdeera.API.Contracts.Requests;

public class UpdatePreferencesRequest
{
    public string? PreferredLanguage { get; init; }
    public string? PreferredTheme { get; init; }
}
