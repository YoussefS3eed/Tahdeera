using FluentAssertions;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tahdeera.Application.Features.Auth.DTOs;
using Tahdeera.IntegrationTests.Fixtures;

namespace Tahdeera.IntegrationTests.Helpers;

/// <summary>
/// Authentic HTTP login helper. Performs genuine POST /api/auth/login requests
/// using persona credentials to acquire authentic JWT tokens and attach them to HttpClient.
/// </summary>
public static class AuthenticationHelper
{
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        this CustomWebApplicationFactory factory,
        string usernameOrEmailOrPhone,
        string password)
    {
        var client = factory.CreateClient();

        var loginPayload = new
        {
            usernameOrEmailOrPhone = usernameOrEmailOrPhone,
            password = password
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginPayload, JsonSerializationHelper.Options);
        response.EnsureSuccessStatusCode();

        var authResult = await response.Content.ReadFromJsonAsync<AuthResultDto>(JsonSerializationHelper.Options);
        authResult.Should().NotBeNull();
        authResult!.AccessToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        return client;
    }

    public static async Task<HttpClient> CreateAdminClientAsync(this CustomWebApplicationFactory factory)
    {
        return await factory.CreateAuthenticatedClientAsync("admin@tahdeera.app", "Admin123!456");
    }
}
