using FluentAssertions;
using System.Xml.Linq;

namespace Tahdeera.Application.UnitTests.Finances;

public class DomainMessagesLocalizationTests
{
    private static readonly string[] RequiredFinancialErrorCodes =
    [
        "FINANCES_UNAUTHORIZED_ROLE",
        "STUDENT_NOT_IN_WORKSPACE",
        "STUDENT_ACCOUNT_NOT_FOUND",
        "CHARGE_NOT_FOUND",
        "PAYMENT_NOT_FOUND",
        "AMOUNT_MUST_BE_POSITIVE",
        "CHARGE_DESCRIPTION_REQUIRED",
        "CANCELLATION_REASON_TOO_SHORT",
        "REVERSAL_REASON_TOO_SHORT",
        "CANNOT_CANCEL_NON_OPEN_CHARGE",
        "CANNOT_WRITEOFF_NON_OPEN_CHARGE",
        "PAYMENT_ALREADY_REVERSED",
        "INVALID_PAYMENT_METHOD",
        "PAYMENT_DATE_CANNOT_BE_FUTURE",
        "ALLOCATION_EXCEEDS_PAYMENT",
        "ALLOCATION_EXCEEDS_CHARGE_BALANCE",
        "GROUP_NOT_FOUND_IN_WORKSPACE"
    ];

    private static string FindResourceFilePath(string fileName)
    {
        var currentDir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(currentDir))
        {
            var candidate = Path.Combine(currentDir, "src", "Tahdeera.Application", "Resources", fileName);
            if (File.Exists(candidate))
                return candidate;

            var parent = Directory.GetParent(currentDir);
            if (parent == null) break;
            currentDir = parent.FullName;
        }

        // Fallback relative to project root
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Tahdeera.Application/Resources", fileName));
    }

    [Theory]
    [InlineData("DomainMessages.ar.resx")]
    [InlineData("DomainMessages.en.resx")]
    public void Resx_ShouldContainAllSeventeenFinancialErrorCodes(string resxFileName)
    {
        // Arrange
        var filePath = FindResourceFilePath(resxFileName);
        File.Exists(filePath).Should().BeTrue($"Resource file must exist at {filePath}");

        var doc = XDocument.Load(filePath);
        var keys = doc.Root?.Elements("data")
            .Select(e => e.Attribute("name")?.Value)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToHashSet() ?? new HashSet<string?>();

        // Act & Assert
        foreach (var code in RequiredFinancialErrorCodes)
        {
            keys.Should().Contain(code, $"Error code {code} must be present in {resxFileName}");
            keys.Should().Contain($"{code}_Title", $"Error code {code}_Title must be present in {resxFileName}");
            keys.Should().Contain($"{code}_Detail", $"Error code {code}_Detail must be present in {resxFileName}");
        }
    }
}
