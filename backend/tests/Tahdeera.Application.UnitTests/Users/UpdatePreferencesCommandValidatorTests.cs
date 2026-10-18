using FluentAssertions;
using Tahdeera.Application.Features.Users.Commands.UpdatePreferences;

namespace Tahdeera.Application.UnitTests.Users;

public class UpdatePreferencesCommandValidatorTests
{
    private readonly UpdatePreferencesCommandValidator _validator = new();

    [Theory]
    [InlineData("ar", "dark")]
    [InlineData("en", "light")]
    [InlineData("ar", null)]
    [InlineData(null, "dark")]
    public void Validate_ValidInputs_ShouldPass(string? lang, string? theme)
    {
        var command = new UpdatePreferencesCommand(Guid.NewGuid(), lang, theme);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_BothNull_ShouldFailWithAtLeastOneRequired()
    {
        var command = new UpdatePreferencesCommand(Guid.NewGuid(), null, null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "AT_LEAST_ONE_PREFERENCE_REQUIRED");
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    public void Validate_InvalidLanguage_ShouldFail(string lang)
    {
        var command = new UpdatePreferencesCommand(Guid.NewGuid(), lang, null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_LANGUAGE");
    }

    [Theory]
    [InlineData("system")]
    [InlineData("blue")]
    [InlineData("solarized")]
    public void Validate_InvalidTheme_ShouldFail(string theme)
    {
        var command = new UpdatePreferencesCommand(Guid.NewGuid(), null, theme);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_THEME");
    }
}
