using FluentAssertions;
using System.Net;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.UnitTests.Entities;

public class AppUserAndRefreshTokenTests
{
    [Fact]
    public void AppUser_DefaultInitialization_SetsDefaultValues()
    {
        // Act
        var user = new AppUser();

        // Assert
        user.Id.Should().NotBeEmpty();
        user.PreferredLanguage.Should().Be("ar");
        user.PreferredTheme.Should().Be("light");
        user.IsPlatformAdmin.Should().BeFalse();
        user.IsActive.Should().BeTrue();
        user.LastLoginAt.Should().BeNull();
        user.RefreshTokens.Should().NotBeNull().And.BeEmpty();
        user.WorkspaceMemberships.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("ar", "light")]
    [InlineData("ar", "dark")]
    [InlineData("en", "light")]
    [InlineData("en", "dark")]
    public void UpdatePreferences_WithValidCombinations_UpdatesPreferences(string lang, string theme)
    {
        // Arrange
        var user = new AppUser();

        // Act
        user.UpdatePreferences(lang, theme);

        // Assert
        user.PreferredLanguage.Should().Be(lang);
        user.PreferredTheme.Should().Be(theme);
        user.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("")]
    public void UpdatePreferences_WithInvalidLanguage_ThrowsDomainException(string invalidLang)
    {
        // Arrange
        var user = new AppUser();

        // Act
        var act = () => user.UpdatePreferences(invalidLang, "light");

        // Assert
        act.Should().Throw<DomainException>()
            .Where(e => e.ErrorCode == "INVALID_LANGUAGE" && e.StatusCode == HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("blue")]
    [InlineData("solarized")]
    [InlineData("")]
    public void UpdatePreferences_WithInvalidTheme_ThrowsDomainException(string invalidTheme)
    {
        // Arrange
        var user = new AppUser();

        // Act
        var act = () => user.UpdatePreferences("ar", invalidTheme);

        // Assert
        act.Should().Throw<DomainException>()
            .Where(e => e.ErrorCode == "INVALID_THEME" && e.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public void RecordLogin_SetsLastLoginAtAndUpdatedAt()
    {
        // Arrange
        var user = new AppUser();
        var loginTime = DateTimeOffset.UtcNow;

        // Act
        user.RecordLogin(loginTime);

        // Assert
        user.LastLoginAt.Should().Be(loginTime);
        user.UpdatedAt.Should().Be(loginTime);
    }

    [Fact]
    public void RefreshToken_Create_SetsPropertiesAndIsActiveTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenHash = new byte[] { 1, 2, 3, 4, 5 };
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        var token = RefreshToken.Create(userId, tokenHash, expiresAt);

        // Assert
        token.Id.Should().NotBeEmpty();
        token.UserId.Should().Be(userId);
        token.TokenHash.Should().Equal(tokenHash);
        token.ExpiresAt.Should().Be(expiresAt);
        token.RevokedAt.Should().BeNull();
        token.ReplacedByTokenHash.Should().BeNull();
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RefreshToken_WhenExpired_IsActiveReturnsFalse()
    {
        // Arrange
        var token = RefreshToken.Create(Guid.NewGuid(), [1, 2], DateTimeOffset.UtcNow.AddMinutes(-5));

        // Assert
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_Revoke_SetsRevokedAtAndReplacedByTokenHash()
    {
        // Arrange
        var token = RefreshToken.Create(Guid.NewGuid(), [1, 2], DateTimeOffset.UtcNow.AddDays(1));
        var revokeTime = DateTimeOffset.UtcNow;
        var replacementHash = new byte[] { 9, 8, 7 };

        // Act
        token.Revoke(revokeTime, replacementHash);

        // Assert
        token.RevokedAt.Should().Be(revokeTime);
        token.ReplacedByTokenHash.Should().Equal(replacementHash);
        token.UpdatedAt.Should().Be(revokeTime);
        token.IsActive.Should().BeFalse();
    }
}
