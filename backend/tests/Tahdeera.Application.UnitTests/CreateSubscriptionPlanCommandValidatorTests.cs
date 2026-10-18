using FluentAssertions;
using Tahdeera.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.DTOs;

namespace Tahdeera.Application.UnitTests;

public class CreateSubscriptionPlanCommandValidatorTests
{
    private readonly CreateSubscriptionPlanCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        var command = new CreateSubscriptionPlanCommand(
            Code: "TEACHER_PRO",
            NameAr: "باقة المعلم المحترف",
            NameEn: "Teacher Pro",
            DescriptionAr: "وصف",
            DescriptionEn: "Description",
            DefaultPrice: 1500.00m,
            Currency: "EGP",
            DurationDays: 365,
            MaxStudents: 200,
            MaxStaff: 2,
            MaxBranches: 1,
            Features: new SubscriptionPlanFeaturesDto());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyCode_ShouldFail()
    {
        var command = new CreateSubscriptionPlanCommand(
            Code: "",
            NameAr: "باقة",
            NameEn: "Plan",
            DescriptionAr: null,
            DescriptionEn: null,
            DefaultPrice: 500,
            Currency: "EGP",
            DurationDays: 365,
            MaxStudents: 100,
            MaxStaff: 1,
            MaxBranches: 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_NegativePrice_ShouldFail()
    {
        var command = new CreateSubscriptionPlanCommand(
            Code: "PLAN_NEG",
            NameAr: "باقة",
            NameEn: "Plan",
            DescriptionAr: null,
            DescriptionEn: null,
            DefaultPrice: -50.00m,
            Currency: "EGP",
            DurationDays: 365,
            MaxStudents: 100,
            MaxStaff: 1,
            MaxBranches: 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DefaultPrice");
    }

    [Fact]
    public void Validate_ZeroMaxStudents_ShouldFail()
    {
        var command = new CreateSubscriptionPlanCommand(
            Code: "PLAN_ZERO_STUDENTS",
            NameAr: "باقة",
            NameEn: "Plan",
            DescriptionAr: null,
            DescriptionEn: null,
            DefaultPrice: 100,
            Currency: "EGP",
            DurationDays: 365,
            MaxStudents: 0,
            MaxStaff: 1,
            MaxBranches: 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxStudents");
    }
}
