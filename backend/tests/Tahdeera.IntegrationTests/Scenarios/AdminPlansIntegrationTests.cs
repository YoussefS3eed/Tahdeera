using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class AdminPlansIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminPlansIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> CreateWorkspaceOwnerClientAsync()
    {
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "TEACHER_STARTER");

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("plans_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("PlanTestCenter"),
            Kind: "Teacher",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo, Egypt",
            DefaultLateGraceMinutes: 15,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Plan Test Owner",
                Phone: TestDataGenerator.GenerateEgyptianPhone()
            ),
            Subscription: new CreateInitialSubscriptionDto(
                PlanId: plan.Id,
                StartsAt: DateTimeOffset.UtcNow,
                EndsAt: DateTimeOffset.UtcNow.AddDays(365),
                AgreedPrice: plan.DefaultPrice,
                InitialPayment: new CreateInitialPaymentDto(plan.DefaultPrice, "Cash", null, null)
            )
        );

        var wsResponse = await adminClient.PostAsJsonAsync("/api/admin/workspaces", createWsCommand, JsonSerializationHelper.Options);
        wsResponse.EnsureSuccessStatusCode();

        return await _factory.CreateAuthenticatedClientAsync(ownerEmail, ownerPassword);
    }

    [Fact]
    public async Task GetPlans_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/plans");

        // Assert: 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPlans_AsWorkspaceOwner_ReturnsForbidden()
    {
        // Arrange
        var ownerClient = await CreateWorkspaceOwnerClientAsync();

        // Act
        var response = await ownerClient.GetAsync("/api/admin/plans");

        // Assert: 403 Forbidden (Non-PlatformAdmin blocked by [Authorize(Policy = "PlatformAdmin")])
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPlans_AsSuperAdmin_ReturnsOkWithSeededPlans()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/admin/plans");

        // Assert: 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plans = await response.Content.ReadFromJsonAsync<List<SubscriptionPlanDto>>(JsonSerializationHelper.Options);
        plans.Should().NotBeNull();
        plans!.Count.Should().BeGreaterThanOrEqualTo(2);
        plans.Should().Contain(p => p.Code == "TEACHER_STARTER");
    }

    [Fact]
    public async Task GetPlans_ActiveOnlyFilter_ReturnsOnlyActivePlans()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/admin/plans?activeOnly=true");

        // Assert: 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plans = await response.Content.ReadFromJsonAsync<List<SubscriptionPlanDto>>(JsonSerializationHelper.Options);
        plans.Should().NotBeNull();
        plans!.All(p => p.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task CreatePlan_AsWorkspaceOwner_ReturnsForbidden()
    {
        // Arrange
        var ownerClient = await CreateWorkspaceOwnerClientAsync();
        var command = new CreateSubscriptionPlanCommand(
            Code: "FORBIDDEN_PLAN",
            NameAr: "خطة غير مصرح بها",
            NameEn: "Unauthorized Plan",
            DescriptionAr: null,
            DescriptionEn: null,
            DefaultPrice: 500m
        );

        // Act
        var response = await ownerClient.PostAsJsonAsync("/api/admin/plans", command, JsonSerializationHelper.Options);

        // Assert: 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateAndLifecyclePlan_AsSuperAdmin_SucceedsWithPersistenceVerification()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        var uniqueCode = "PLAN_" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        var createCommand = new CreateSubscriptionPlanCommand(
            Code: uniqueCode,
            NameAr: "خطة تجريبية متكاملة",
            NameEn: "Integrated Test Plan",
            DescriptionAr: "وصف تجريبي",
            DescriptionEn: "Test description",
            DefaultPrice: 650.00m,
            Currency: "EGP",
            DurationDays: 180,
            MaxStudents: 220,
            MaxStaff: 4,
            MaxBranches: 2,
            Features: new SubscriptionPlanFeaturesDto(
                StudentsAndGroups: true,
                NfcAttendance: true,
                PaymentsAndFees: true,
                WhatsappNotifications: true,
                ExamsAndQuestionBank: false,
                HomeworkAndLibrary: false,
                PerformanceReports: true,
                AiAssistant: false,
                ExcelExport: true,
                TeamPermissions: true
            )
        );

        // Act 1: Create Plan
        var createResponse = await adminClient.PostAsJsonAsync("/api/admin/plans", createCommand, JsonSerializationHelper.Options);

        // Assert 1: 201 Created
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPlan = await createResponse.Content.ReadFromJsonAsync<SubscriptionPlanDto>(JsonSerializationHelper.Options);
        createdPlan.Should().NotBeNull();
        createdPlan!.Code.Should().Be(uniqueCode);
        createdPlan.DefaultPrice.Should().Be(650.00m);
        createdPlan.MaxStudents.Should().Be(220);

        // Verify in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
            var dbPlan = await db.SubscriptionPlans.FindAsync(createdPlan.Id);
            dbPlan.Should().NotBeNull();
            dbPlan!.Code.Should().Be(uniqueCode);
            dbPlan.DefaultPrice.Should().Be(650.00m);
        }

        // Act 2: Update Plan
        var updateRequest = new UpdatePlanRequest(
            NameAr: "خطة تجريبية محدثة",
            NameEn: "Updated Test Plan",
            DescriptionAr: "وصف محدث",
            DescriptionEn: "Updated description",
            DefaultPrice: 800.00m,
            Currency: "EGP",
            DurationDays: 365,
            MaxStudents: 300,
            MaxStaff: 6,
            MaxBranches: 3,
            Features: new SubscriptionPlanFeaturesDto(
                StudentsAndGroups: true,
                NfcAttendance: true,
                PaymentsAndFees: true,
                WhatsappNotifications: true,
                ExamsAndQuestionBank: true,
                HomeworkAndLibrary: true,
                PerformanceReports: true,
                AiAssistant: true,
                ExcelExport: true,
                TeamPermissions: true
            ),
            IsActive: true
        );

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/admin/plans/{createdPlan.Id}", updateRequest, JsonSerializationHelper.Options);

        // Assert 2: 200 OK
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedPlan = await updateResponse.Content.ReadFromJsonAsync<SubscriptionPlanDto>(JsonSerializationHelper.Options);
        updatedPlan.Should().NotBeNull();
        updatedPlan!.DefaultPrice.Should().Be(800.00m);
        updatedPlan.MaxStudents.Should().Be(300);

        // Verify in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
            var dbPlan = await db.SubscriptionPlans.FindAsync(createdPlan.Id);
            dbPlan.Should().NotBeNull();
            dbPlan!.DefaultPrice.Should().Be(800.00m);
            dbPlan.MaxStudents.Should().Be(300);
            dbPlan.NameAr.Should().Be("خطة تجريبية محدثة");
        }
    }
}
