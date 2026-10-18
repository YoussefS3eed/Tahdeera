using FluentAssertions;
using Tahdeera.Application.Features.Auth.Commands.RefreshToken;

namespace Tahdeera.Application.UnitTests.Auth;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        var command = new RefreshTokenCommand("valid-token-string");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyRefreshToken_ShouldFail(string? token)
    {
        var command = new RefreshTokenCommand(token!);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "REFRESH_TOKEN_REQUIRED");
    }
}
