using FluentAssertions;
using System.Net;
using Tahdeera.IntegrationTests.Fixtures;

namespace Tahdeera.IntegrationTests.Endpoints;

[Collection("IntegrationTests")]
public class HeaderApiVersioningIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public HeaderApiVersioningIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Request_With_XApiVersion_Header_Succeeds()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/reference/grades");
        request.Headers.Add("X-Api-Version", "1.0");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("api-supported-versions").Should().BeTrue();
    }

    [Fact]
    public async Task Request_With_api_version_Header_Succeeds()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/reference/grades");
        request.Headers.Add("api-version", "1.0");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_Without_Version_Header_Assumes_Default_1_0()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/reference/grades");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_With_Unsupported_Api_Version_Returns_BadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/reference/grades");
        request.Headers.Add("X-Api-Version", "99.0");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
