using FluentAssertions;
using System.Globalization;
using System.Resources;
using Tahdeera.Application.Resources;

namespace Tahdeera.Application.UnitTests.Localization;

public class DomainMessagesLocalizationTests
{
    private static readonly string[] B3ErrorCodes =
    [
        "GROUP_NAME_DUPLICATE",
        "GROUP_NOT_FOUND",
        "INVALID_GRADE_ID",
        "INVALID_ABSENCE_THRESHOLD",
        "GROUP_GRADE_MISMATCH",
        "STUDENT_WORKSPACE_STATUS_NOT_ACTIVE",
        "STUDENT_ALREADY_ENROLLED_IN_GROUP",
        "STUDENT_NOT_ENROLLED_IN_GROUP",
        "SESSION_NOT_FOUND",
        "SESSION_INVALID_TIME_RANGE",
        "SESSION_ALREADY_COMPLETED_SCHEDULE_FROZEN",
        "CANCELLATION_REASON_REQUIRED",
        "CREDENTIAL_NOT_FOUND",
        "CREDENTIAL_REVOKED",
        "STUDENT_BLOCKED_IN_WORKSPACE",
        "ATTENDANCE_CORRECTION_WINDOW_EXPIRED",
        "CORRECTION_REASON_REQUIRED",
        "RETROACTIVE_REASON_REQUIRED",
        "UNAUTHORIZED_WORKSPACE_ACCESS",
        "ASSISTANT_NOT_AUTHORIZED_DELEGATE"
    ];

    private readonly ResourceManager _resourceManager = new(
        "Tahdeera.Application.Resources.DomainMessages",
        typeof(DomainMessages).Assembly);

    [Fact]
    public void Resource_Catalog_Contains_All_20_B3_Error_Codes_In_Arabic()
    {
        var culture = new CultureInfo("ar");

        foreach (var code in B3ErrorCodes)
        {
            var value = _resourceManager.GetString(code, culture);
            value.Should().NotBeNullOrWhiteSpace($"Key '{code}' must exist in Arabic resource catalog.");
        }
    }

    [Fact]
    public void Resource_Catalog_Contains_All_20_B3_Error_Codes_In_English()
    {
        var culture = new CultureInfo("en");

        foreach (var code in B3ErrorCodes)
        {
            var value = _resourceManager.GetString(code, culture);
            value.Should().NotBeNullOrWhiteSpace($"Key '{code}' must exist in English resource catalog.");
        }
    }

    [Theory]
    [InlineData("ar")]
    [InlineData("en")]
    public void Resource_Catalog_Keys_Have_Title_And_Detail_Variants(string cultureCode)
    {
        var culture = new CultureInfo(cultureCode);

        foreach (var code in B3ErrorCodes)
        {
            var titleKey = $"{code}_Title";
            var detailKey = $"{code}_Detail";

            var titleValue = _resourceManager.GetString(titleKey, culture);
            var detailValue = _resourceManager.GetString(detailKey, culture);

            titleValue.Should().NotBeNullOrWhiteSpace($"Key '{titleKey}' must exist in {cultureCode} catalog.");
            detailValue.Should().NotBeNullOrWhiteSpace($"Key '{detailKey}' must exist in {cultureCode} catalog.");
        }
    }
}
