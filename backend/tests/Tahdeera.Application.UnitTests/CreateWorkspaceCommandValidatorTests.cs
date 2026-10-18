using FluentAssertions;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;

namespace Tahdeera.Application.UnitTests;

public class CreateWorkspaceCommandValidatorTests
{
    private readonly CreateWorkspaceCommandValidator _validator = new();

    private CreateWorkspaceCommand CreateValidCommand()
    {
        return new CreateWorkspaceCommand(
            Name: "سنتر النخبة",
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: "01012345678",
            CityAddress: "القاهرة",
            DefaultLateGraceMinutes: 20,
            Owner: new CreateOwnerDto(
                Username: "el_nokhba",
                Email: "owner@nokhba.com",
                Password: "Password123!",
                DisplayName: "أحمد علي",
                Phone: "01012345678"),
            Subscription: new CreateInitialSubscriptionDto(
                PlanId: Guid.NewGuid(),
                StartsAt: DateTimeOffset.UtcNow,
                EndsAt: DateTimeOffset.UtcNow.AddYears(1),
                AgreedPrice: 3000.00m,
                InitialPayment: new CreateInitialPaymentDto(
                    Amount: 3000.00m,
                    Method: "Cash",
                    Reference: "REF-001",
                    Notes: "Initial payment")));
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        var command = CreateValidCommand();
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidKind_ShouldFail()
    {
        var command = CreateValidCommand() with { Kind = "InvalidKind" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Kind");
    }

    [Fact]
    public void Validate_PhoneLongerThan11Digits_ShouldFail()
    {
        var command = CreateValidCommand() with { ContactPhone = "0101234567899" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContactPhone");
    }

    [Fact]
    public void Validate_PasswordShorterThan6Chars_ShouldFail()
    {
        var valid = CreateValidCommand();
        var command = valid with
        {
            Owner = valid.Owner with { Password = "123" }
        };

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Owner.Password");
    }

    [Fact]
    public void Validate_EndsAtBeforeStartsAt_ShouldFail()
    {
        var valid = CreateValidCommand();
        var now = DateTimeOffset.UtcNow;
        var command = valid with
        {
            Subscription = valid.Subscription with
            {
                StartsAt = now,
                EndsAt = now.AddDays(-1)
            }
        };

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subscription.EndsAt");
    }

    [Fact]
    public void Validate_NegativePaymentAmount_ShouldFail()
    {
        var valid = CreateValidCommand();
        var command = valid with
        {
            Subscription = valid.Subscription with
            {
                InitialPayment = valid.Subscription.InitialPayment with { Amount = -100m }
            }
        };

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subscription.InitialPayment.Amount");
    }
}
