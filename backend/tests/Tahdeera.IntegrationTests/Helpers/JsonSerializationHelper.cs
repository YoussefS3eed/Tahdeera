using System.Text.Json;

namespace Tahdeera.IntegrationTests.Helpers;

/// <summary>
/// Pre-configured JSON serialization options matching ASP.NET Core API conventions.
/// </summary>
public static class JsonSerializationHelper
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
