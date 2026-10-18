using FluentAssertions;
using Tahdeera.Application.Features.Auth.Commands.Login;

namespace Tahdeera.Application.UnitTests.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        var command = new LoginCommand("teacher@tahdeera.app", "Password123!");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyUsernameOrEmailOrPhone_ShouldFail(string? identifier)
    {
        var command = new LoginCommand(identifier!, "Password123!");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "USERNAME_OR_EMAIL_REQUIRED");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyPassword_ShouldFail(string? password)
    {
        var command = new LoginCommand("teacher@tahdeera.app", password!);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PASSWORD_REQUIRED");
    }
}
