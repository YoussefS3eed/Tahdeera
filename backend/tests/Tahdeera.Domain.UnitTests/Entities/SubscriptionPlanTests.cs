using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.ValueObjects;

namespace Tahdeera.Domain.UnitTests.Entities;

public class SubscriptionPlanTests
{
    [Fact]
    public void SubscriptionPlan_DefaultInitialization_SetsExpectedDefaults()
    {
        // Act
        var plan = new SubscriptionPlan();

        // Assert
        plan.Id.Should().NotBeEmpty();
        plan.Code.Should().BeEmpty();
        plan.NameAr.Should().BeEmpty();
        plan.NameEn.Should().BeEmpty();
        plan.DefaultPrice.Should().Be(0m);
        plan.Currency.Should().Be("EGP");
        plan.DurationDays.Should().Be(365);
        plan.MaxStudents.Should().Be(150);
        plan.MaxStaff.Should().Be(1);
        plan.MaxBranches.Should().Be(1);
        plan.IsActive.Should().BeTrue();
        plan.Features.Should().NotBeNull();
        plan.Subscriptions.Should().NotBeNull().And.BeEmpty();
        plan.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        plan.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SubscriptionPlan_PropertyMutation_RetainsAssignedValues()
    {
        // Arrange & Act
        var customFeatures = new SubscriptionPlanFeatures(
            StudentsAndGroups: true,
            NfcAttendance: true,
            PaymentsAndFees: true,
            WhatsappNotifications: true,
            ExamsAndQuestionBank: true,
            HomeworkAndLibrary: true,
            PerformanceReports: true,
            AiAssistant: true,
            ExcelExport: true,
            TeamPermissions: true);

        var plan = new SubscriptionPlan
        {
            Code = "ENTERPRISE_CENTER",
            NameAr = "خطة المراكز الكبرى",
            NameEn = "Enterprise Center Plan",
            DescriptionAr = "وصف الخطة بالعربية",
            DescriptionEn = "Plan description in English",
            DefaultPrice = 2500m,
            Currency = "EGP",
            DurationDays = 180,
            MaxStudents = 1000,
            MaxStaff = 10,
            MaxBranches = 5,
            Features = customFeatures,
            IsActive = false
        };

        // Assert
        plan.Code.Should().Be("ENTERPRISE_CENTER");
        plan.NameAr.Should().Be("خطة المراكز الكبرى");
        plan.NameEn.Should().Be("Enterprise Center Plan");
        plan.DescriptionAr.Should().Be("وصف الخطة بالعربية");
        plan.DescriptionEn.Should().Be("Plan description in English");
        plan.DefaultPrice.Should().Be(2500m);
        plan.Currency.Should().Be("EGP");
        plan.DurationDays.Should().Be(180);
        plan.MaxStudents.Should().Be(1000);
        plan.MaxStaff.Should().Be(10);
        plan.MaxBranches.Should().Be(5);
        plan.IsActive.Should().BeFalse();
        plan.Features.AiAssistant.Should().BeTrue();
        plan.Features.TeamPermissions.Should().BeTrue();
        plan.Features.ExamsAndQuestionBank.Should().BeTrue();
    }
}
