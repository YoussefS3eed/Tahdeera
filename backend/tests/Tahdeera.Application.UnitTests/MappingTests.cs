using FluentAssertions;
using Mapster;
using Tahdeera.Application.Common.Mappings;
using Tahdeera.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.Commands.UpdateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.ValueObjects;

namespace Tahdeera.Application.UnitTests;

public class MappingTests
{
    public MappingTests()
    {
        var config = TypeAdapterConfig.GlobalSettings;
        new MappingConfig().Register(config);
    }

    [Fact]
    public void Map_CreateSubscriptionPlanCommand_To_SubscriptionPlan_ShouldMapCorrectly()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Code: " teacher_pro ",
            NameAr: " باقة المعلم المحترف ",
            NameEn: " Teacher Pro ",
            DescriptionAr: " وصف ",
            DescriptionEn: " Description ",
            DefaultPrice: 1500.00m,
            Currency: "egp",
            DurationDays: 365,
            MaxStudents: 200,
            MaxStaff: 2,
            MaxBranches: 1,
            Features: new SubscriptionPlanFeaturesDto(AiAssistant: true));

        // Act
        var plan = command.Adapt<SubscriptionPlan>();

        // Assert
        plan.Code.Should().Be("TEACHER_PRO");
        plan.NameAr.Should().Be("باقة المعلم المحترف");
        plan.NameEn.Should().Be("Teacher Pro");
        plan.DescriptionAr.Should().Be("وصف");
        plan.DescriptionEn.Should().Be("Description");
        plan.DefaultPrice.Should().Be(1500.00m);
        plan.Currency.Should().Be("EGP");
        plan.DurationDays.Should().Be(365);
        plan.MaxStudents.Should().Be(200);
        plan.MaxStaff.Should().Be(2);
        plan.MaxBranches.Should().Be(1);
        plan.IsActive.Should().BeTrue();
        plan.Features.AiAssistant.Should().BeTrue();
    }

    [Fact]
    public void Map_SubscriptionPlan_To_SubscriptionPlanDto_ShouldMapCorrectly()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Id = planId,
            Code = "STARTER",
            NameAr = "أساسي",
            NameEn = "Starter",
            DescriptionAr = "وصف",
            DescriptionEn = "Desc",
            DefaultPrice = 500m,
            Currency = "EGP",
            DurationDays = 30,
            MaxStudents = 50,
            MaxStaff = 1,
            MaxBranches = 1,
            Features = new SubscriptionPlanFeatures(NfcAttendance: true, PerformanceReports: true),
            IsActive = true
        };

        // Act
        var dto = plan.Adapt<SubscriptionPlanDto>();

        // Assert
        dto.Id.Should().Be(planId);
        dto.Code.Should().Be("STARTER");
        dto.NameAr.Should().Be("أساسي");
        dto.NameEn.Should().Be("Starter");
        dto.DefaultPrice.Should().Be(500m);
        dto.Features.NfcAttendance.Should().BeTrue();
        dto.Features.PerformanceReports.Should().BeTrue();
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Adapt_UpdateSubscriptionPlanCommand_InPlace_ShouldUpdatePlan()
    {
        // Arrange
        var plan = new SubscriptionPlan
        {
            Code = "TEST",
            NameAr = "قديم",
            NameEn = "Old",
            DefaultPrice = 100m,
            Currency = "EGP",
            DurationDays = 30,
            MaxStudents = 10,
            MaxStaff = 1,
            MaxBranches = 1,
            Features = new SubscriptionPlanFeatures(),
            IsActive = true
        };

        var updateCommand = new UpdateSubscriptionPlanCommand(
            Id: plan.Id,
            NameAr: " جديد ",
            NameEn: " New ",
            DescriptionAr: null,
            DescriptionEn: null,
            DefaultPrice: 200m,
            Currency: "usd",
            DurationDays: 60,
            MaxStudents: 25,
            MaxStaff: 3,
            MaxBranches: 2,
            Features: new SubscriptionPlanFeaturesDto(WhatsappNotifications: true),
            IsActive: false);

        // Act
        updateCommand.Adapt(plan);

        // Assert
        plan.NameAr.Should().Be("جديد");
        plan.NameEn.Should().Be("New");
        plan.DefaultPrice.Should().Be(200m);
        plan.Currency.Should().Be("USD");
        plan.DurationDays.Should().Be(60);
        plan.MaxStudents.Should().Be(25);
        plan.MaxStaff.Should().Be(3);
        plan.MaxBranches.Should().Be(2);
        plan.IsActive.Should().BeFalse();
        plan.Features.WhatsappNotifications.Should().BeTrue();
    }
}
