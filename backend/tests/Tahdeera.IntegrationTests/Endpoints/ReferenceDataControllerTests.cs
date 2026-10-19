using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.Application.Features.ReferenceData.DTOs;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Endpoints;

[Collection("IntegrationTests")]
public class ReferenceDataControllerTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public ReferenceDataControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetGrades_Anonymous_ShouldReturnAll12EgyptianGrades()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/reference/grades");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var grades = await response.Content.ReadFromJsonAsync<List<GradeDto>>(JsonSerializationHelper.Options);
        grades.Should().NotBeNull();
        grades!.Count.Should().Be(12);
        grades[0].SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task GetAcademicYears_Anonymous_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/reference/academic-years");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task EnsureAcademicYearExistsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Tahdeera.Infrastructure.Persistence.TahdeeraDbContext>();
        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.AcademicYears))
        {
            db.AcademicYears.Add(new Tahdeera.Domain.Entities.AcademicYear
            {
                Name = "2026/2027",
                StartsOn = new DateOnly(2026, 9, 1),
                EndsOn = new DateOnly(2027, 6, 30),
                IsCurrent = true
            });
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetAcademicYears_Authenticated_ShouldReturnAcademicYears()
    {
        // Arrange
        await EnsureAcademicYearExistsAsync();
        var adminClient = await _factory.CreateAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/reference/academic-years");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var years = await response.Content.ReadFromJsonAsync<List<AcademicYearDto>>(JsonSerializationHelper.Options);
        years.Should().NotBeNull();
        years.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCurrentAcademicYear_Authenticated_ShouldReturnCurrentYear()
    {
        // Arrange
        await EnsureAcademicYearExistsAsync();
        var adminClient = await _factory.CreateAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/reference/academic-years/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var currentYear = await response.Content.ReadFromJsonAsync<AcademicYearDto>(JsonSerializationHelper.Options);
        currentYear.Should().NotBeNull();
        currentYear!.IsCurrent.Should().BeTrue();
    }
}
