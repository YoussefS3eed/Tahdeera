using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.Application.Features.Auth.DTOs;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class AuthenticationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndProfile()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginPayload = new
        {
            usernameOrEmailOrPhone = "admin@tahdeera.app",
            password = "Admin123!456"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginPayload, JsonSerializationHelper.Options);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResult = await response.Content.ReadFromJsonAsync<AuthResultDto>(JsonSerializationHelper.Options);
        authResult.Should().NotBeNull();
        authResult!.AccessToken.Should().NotBeNullOrWhiteSpace();
        authResult.RefreshToken.Should().NotBeNullOrWhiteSpace();
        authResult.User.Username.Should().Be("admin");
        authResult.User.IsPlatformAdmin.Should().BeTrue();

        // Assert Database state in PostgreSQL
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var tokenInDb = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == authResult.User.Id && t.RevokedAt == null);
        tokenInDb.Should().NotBeNull();
        tokenInDb!.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401ProblemDetails()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginPayload = new
        {
            usernameOrEmailOrPhone = "admin@tahdeera.app",
            password = "WrongPassword123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginPayload, JsonSerializationHelper.Options);

        // Assert RFC 9457 ProblemDetails
        var problem = await response.ShouldHaveProblemDetails(HttpStatusCode.Unauthorized, "AUTH_INVALID_CREDENTIALS");
        problem.Detail.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_RotatesTokensAndUpdatesDatabase()
    {
        // Arrange: Login as admin to get initial refresh token
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmailOrPhone = "admin@tahdeera.app",
            password = "Admin123!456"
        }, JsonSerializationHelper.Options);
        loginResponse.EnsureSuccessStatusCode();
        var initialAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResultDto>(JsonSerializationHelper.Options);

        // Act: Refresh token
        var refreshPayload = new { refreshToken = initialAuth!.RefreshToken };
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", refreshPayload, JsonSerializationHelper.Options);

        // Assert HTTP response
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthTokenDto>(JsonSerializationHelper.Options);
        rotatedAuth.Should().NotBeNull();
        rotatedAuth!.AccessToken.Should().NotBeNullOrWhiteSpace();
        rotatedAuth.RefreshToken.Should().NotBeNullOrWhiteSpace();
        rotatedAuth.RefreshToken.Should().NotBe(initialAuth.RefreshToken);

        // Assert Database state: Old token revoked, new token active
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var userTokens = await db.RefreshTokens
            .Where(t => t.UserId == initialAuth.User.Id)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        userTokens.Should().HaveCount(2);
        var oldToken = userTokens[0];
        var newToken = userTokens[1];

        oldToken.RevokedAt.Should().NotBeNull();
        oldToken.ReplacedByTokenHash.Should().NotBeNull();
        newToken.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task RefreshToken_ReplayAttack_RevokesEntireTokenFamily()
    {
        // Arrange: Login -> Refresh (old token is rotated and replaced)
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmailOrPhone = "admin@tahdeera.app",
            password = "Admin123!456"
        }, JsonSerializationHelper.Options);
        var initialAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResultDto>(JsonSerializationHelper.Options);

        // Valid refresh rotates initial token
        await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = initialAuth!.RefreshToken }, JsonSerializationHelper.Options);

        // Act: Attacker attempts to replay the already-used first token
        var replayResponse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = initialAuth.RefreshToken }, JsonSerializationHelper.Options);

        // Assert HTTP response: 401 Unauthorized
        await replayResponse.ShouldHaveProblemDetails(HttpStatusCode.Unauthorized, "AUTH_REFRESH_TOKEN_REVOKED");

        // Assert Database breach containment: ALL tokens for this user must now be revoked
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == initialAuth.User.Id && t.RevokedAt == null)
            .ToListAsync();

        activeTokens.Should().BeEmpty("Replay attack must revoke all active sessions for the user family.");
    }
}
