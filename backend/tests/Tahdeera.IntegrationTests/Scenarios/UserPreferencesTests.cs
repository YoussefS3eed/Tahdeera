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
public class UserPreferencesTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public UserPreferencesTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UpdatePreferences_WithValidLanguageAndTheme_PersistsSuccessfully()
    {
        // Arrange
        var client = await _factory.CreateAdminClientAsync();
        var updatePayload = new
        {
            preferredLanguage = "en",
            preferredTheme = "dark"
        };

        // Act: Update preferences
        var updateResponse = await client.PatchAsJsonAsync("/api/users/me/preferences", updatePayload, JsonSerializationHelper.Options);

        // Assert HTTP response
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedProfile = await updateResponse.Content.ReadFromJsonAsync<UserProfileDto>(JsonSerializationHelper.Options);
        updatedProfile.Should().NotBeNull();
        updatedProfile!.PreferredLanguage.Should().Be("en");
        updatedProfile.PreferredTheme.Should().Be("dark");

        // Act: Verify via GET /api/auth/me
        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var meResult = await meResponse.Content.ReadFromJsonAsync<CurrentUserDto>(JsonSerializationHelper.Options);
        meResult.Should().NotBeNull();
        meResult!.User.PreferredLanguage.Should().Be("en");
        meResult.User.PreferredTheme.Should().Be("dark");

        // Assert Database persistence in PostgreSQL
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var userInDb = await db.AppUsers.FirstOrDefaultAsync(u => u.Username == "admin");
        userInDb.Should().NotBeNull();
        userInDb!.PreferredLanguage.Should().Be("en");
        userInDb.PreferredTheme.Should().Be("dark");
    }

    [Fact]
    public async Task UpdatePreferences_WithInvalidLanguage_Returns400ValidationError()
    {
        // Arrange
        var client = await _factory.CreateAdminClientAsync();
        var updatePayload = new
        {
            preferredLanguage = "fr"
        };

        // Act
        var response = await client.PatchAsJsonAsync("/api/users/me/preferences", updatePayload, JsonSerializationHelper.Options);

        // Assert RFC 9457 ProblemDetails with error code VALIDATION_ERROR
        var problem = await response.ShouldHaveProblemDetails(HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        problem.Detail.Should().NotBeNullOrWhiteSpace();

        // Assert Database state remained untouched
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var userInDb = await db.AppUsers.FirstOrDefaultAsync(u => u.Username == "admin");
        userInDb.Should().NotBeNull();
        userInDb!.PreferredLanguage.Should().Be("ar", "Default language should remain unchanged on validation failure.");
    }
}
