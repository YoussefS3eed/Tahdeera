using FluentAssertions;
using Tahdeera.Domain.ValueObjects;

namespace Tahdeera.Domain.UnitTests.ValueObjects;

public class SubscriptionPlanFeaturesTests
{
    [Fact]
    public void SubscriptionPlanFeatures_DefaultConstructor_SetsStandardTierFlags()
    {
        // Act
        var features = new SubscriptionPlanFeatures();

        // Assert
        features.StudentsAndGroups.Should().BeTrue();
        features.NfcAttendance.Should().BeTrue();
        features.PaymentsAndFees.Should().BeTrue();
        features.WhatsappNotifications.Should().BeTrue();
        features.ExamsAndQuestionBank.Should().BeFalse();
        features.HomeworkAndLibrary.Should().BeFalse();
        features.PerformanceReports.Should().BeTrue();
        features.AiAssistant.Should().BeFalse();
        features.ExcelExport.Should().BeTrue();
        features.TeamPermissions.Should().BeFalse();
    }

    [Fact]
    public void SubscriptionPlanFeatures_RecordEquality_WorksByValue()
    {
        // Arrange
        var f1 = new SubscriptionPlanFeatures(AiAssistant: true, TeamPermissions: true);
        var f2 = new SubscriptionPlanFeatures(AiAssistant: true, TeamPermissions: true);
        var f3 = new SubscriptionPlanFeatures(AiAssistant: false, TeamPermissions: true);

        // Assert
        f1.Should().Be(f2);
        (f1 == f2).Should().BeTrue();
        f1.Should().NotBe(f3);
        (f1 == f3).Should().BeFalse();
        f1.GetHashCode().Should().Be(f2.GetHashCode());
    }
}
