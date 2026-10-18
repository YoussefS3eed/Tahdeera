namespace Tahdeera.IntegrationTests.Helpers;

/// <summary>
/// Generates deterministic, valid Egyptian test data conforming to FluentValidation rules.
/// </summary>
public static class TestDataGenerator
{
    public static string GenerateUniqueEmail(string prefix = "user")
        => $"{prefix}_{Guid.NewGuid():N}@tahdeera-test.app";

    public static string GenerateEgyptianPhone()
    {
        var randomSuffix = Random.Shared.Next(10000000, 99999999);
        return $"010{randomSuffix}";
    }

    public static string GenerateWorkspaceName(string prefix = "Center")
        => $"{prefix} {Guid.NewGuid():N}"[..18];
}
