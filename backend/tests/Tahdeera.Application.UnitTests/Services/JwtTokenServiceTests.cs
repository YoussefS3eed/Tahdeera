using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Entities;
using Tahdeera.Infrastructure.Services;

namespace Tahdeera.Application.UnitTests.Services;

public class JwtTokenServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JwtTokenService _service;
    private readonly DateTimeOffset _currentTime;

    public JwtTokenServiceTests()
    {
        _configuration = Substitute.For<IConfiguration>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _currentTime = DateTimeOffset.UtcNow;
        _dateTimeProvider.UtcNow.Returns(_currentTime);
        _configuration["Jwt:SigningKey"].Returns("ThisIsAVeryLongSigningKeyForUnitTestingWith256BitsMinimum!");
        _configuration["Jwt:Issuer"].Returns("https://api.tahdeera.app");
        _configuration["Jwt:Audience"].Returns("https://tahdeera.app");
        _configuration["Jwt:ExpiryMinutes"].Returns("30");

        _service = new JwtTokenService(_configuration, _dateTimeProvider);
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_GeneratesTokenWithClaims()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            DisplayName = "Test User",
            NormalizedEmail = "TEST@TAHDEERA.COM",
            PreferredLanguage = "ar",
            PreferredTheme = "dark",
            IsPlatformAdmin = false
        };

        var (token, expiresAt) = _service.GenerateAccessToken(user);

        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeCloseTo(_currentTime.AddMinutes(30), TimeSpan.FromSeconds(5));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be("https://api.tahdeera.app");
        jwt.Audiences.Should().Contain("https://tahdeera.app");

        var subClaim = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        subClaim.Should().Be(user.Id.ToString());

        var nameClaim = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value;
        nameClaim.Should().Be("testuser");

        var langClaim = jwt.Claims.First(c => c.Type == "preferred_language").Value;
        langClaim.Should().Be("ar");

        var adminClaim = jwt.Claims.First(c => c.Type == "is_platform_admin").Value;
        adminClaim.Should().Be("false");
    }

    [Fact]
    public void GenerateAccessToken_WithWorkspaceAndRoles_IncludesClaims()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "adminuser",
            DisplayName = "Admin",
            NormalizedEmail = "ADMIN@TAHDEERA.COM",
            PreferredLanguage = "en",
            PreferredTheme = "light",
            IsPlatformAdmin = true
        };
        var workspaceId = Guid.NewGuid();
        var roles = new[] { "Teacher", "WorkspaceOwner" };

        var (token, _) = _service.GenerateAccessToken(user, roles, workspaceId);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var wsClaim = jwt.Claims.FirstOrDefault(c => c.Type == "workspace_id");
        wsClaim.Should().NotBeNull();
        wsClaim!.Value.Should().Be(workspaceId.ToString());

        var roleClaims = jwt.Claims.Where(c => c.Type == "role" || c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        roleClaims.Should().Contain("Teacher");
        roleClaims.Should().Contain("WorkspaceOwner");
    }

    [Fact]
    public void GenerateRefreshToken_Returns64ByteBase64String()
    {
        var token1 = _service.GenerateRefreshToken();
        var token2 = _service.GenerateRefreshToken();

        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);

        var bytes = Convert.FromBase64String(token1);
        bytes.Length.Should().Be(64);
    }

    [Fact]
    public void HashToken_ComputesDeterministicSha256()
    {
        var rawToken = "my_refresh_token_sample_12345";
        var hash1 = _service.HashToken(rawToken);
        var hash2 = _service.HashToken(rawToken);

        hash1.Should().Equal(hash2);
        hash1.Length.Should().Be(32); // 256 bits = 32 bytes

        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        hash1.Should().Equal(expected);
    }
}
