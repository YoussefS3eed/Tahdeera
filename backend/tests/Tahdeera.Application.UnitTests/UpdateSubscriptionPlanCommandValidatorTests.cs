using FluentAssertions;
using Tahdeera.Application.Features.Subscriptions.Commands.UpdateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.DTOs;

namespace Tahdeera.Application.UnitTests;

public class UpdateSubscriptionPlanCommandValidatorTests
{
    private readonly UpdateSubscriptionPlanCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        var command = new UpdateSubscriptionPlanCommand(
            Id: Guid.NewGuid(),
            NameAr: "باقة محدثة",
            NameEn: "Updated Plan",
            DescriptionAr: "وصف",
            DescriptionEn: "Desc",
            DefaultPrice: 2000.00m,
            Currency: "EGP",
            DurationDays: 365,
            MaxStudents: 300,
            MaxStaff: 3,
            MaxBranches: 2,
            Features: new SubscriptionPlanFeaturesDto(),
            IsActive: true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyId_ShouldFail()
    {
        var command = new UpdateSubscriptionPlanCommand(
            Id: Guid.Empty,
            NameAr: "باقة",
            NameEn: "Plan",
            DescriptionAr: null,
            DescriptionEn: null,
            DefaultPrice: 100,
            Currency: "EGP",
            DurationDays: 365,
            MaxStudents: 100,
            MaxStaff: 1,
            MaxBranches: 1,
            Features: new SubscriptionPlanFeaturesDto(),
            IsActive: true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
