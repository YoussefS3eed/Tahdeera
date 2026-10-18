using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class SubscriptionLockoutTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public SubscriptionLockoutTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid WorkspaceId, string OwnerEmail, string OwnerPassword, HttpClient OwnerClient)> ProvisionWorkspaceAsync()
    {
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "TEACHER_STARTER");

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("owner");
        var ownerPassword = "OwnerPassword123!";
        var command = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("LockoutTest"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Lockout Center Owner",
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

        var response = await adminClient.PostAsJsonAsync("/api/admin/workspaces", command, JsonSerializationHelper.Options);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateWorkspaceResultDto>(JsonSerializationHelper.Options);

        var ownerClient = await _factory.CreateAuthenticatedClientAsync(ownerEmail, ownerPassword);
        ownerClient.DefaultRequestHeaders.Add("X-Workspace-Id", result!.WorkspaceId.ToString());

        return (result.WorkspaceId, ownerEmail, ownerPassword, ownerClient);
    }

    [Fact]
    public async Task MutatingRequest_WhenWorkspaceSuspended_Returns403WithSupportNumbers()
    {
        // Arrange: Provision workspace, then Super Admin suspends it
        var (workspaceId, _, _, ownerClient) = await ProvisionWorkspaceAsync();
        var adminClient = await _factory.CreateAdminClientAsync();

        var suspendPayload = new { reason = "Audit Compliance Violation" };
        var suspendResponse = await adminClient.PostAsJsonAsync($"/api/admin/workspaces/{workspaceId}/suspend", suspendPayload, JsonSerializationHelper.Options);
        suspendResponse.EnsureSuccessStatusCode();

        var memberRequest = new AddWorkspaceMemberRequest(
            Username: "staff_" + Guid.NewGuid().ToString("N")[..6],
            Email: TestDataGenerator.GenerateUniqueEmail("staff"),
            Password: "Password123!",
            DisplayName: "Teacher",
            Phone: TestDataGenerator.GenerateEgyptianPhone(),
            Role: "Teacher"
        );

        // Act: Owner attempts mutating request on suspended workspace
        var response = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/members", memberRequest, JsonSerializationHelper.Options);

        // Assert: Intercepted by SubscriptionLockoutMiddleware
        var problem = await response.ShouldHaveProblemDetails(HttpStatusCode.Forbidden, "WORKSPACE_SUSPENDED");
        problem.ShouldHaveSupportHotlines();
        problem.SuspensionReason.Should().Be("Audit Compliance Violation");
    }

    [Fact]
    public async Task MutatingRequest_WhenSubscriptionExpired_Returns403SubscriptionExpired()
    {
        // Arrange: Provision workspace, then manually expire its subscription in PostgreSQL
        var (workspaceId, _, _, ownerClient) = await ProvisionWorkspaceAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
            var sub = await db.WorkspaceSubscriptions.FirstAsync(s => s.WorkspaceId == workspaceId);
            sub.StartsAt = DateTimeOffset.UtcNow.AddDays(-30);
            sub.EndsAt = DateTimeOffset.UtcNow.AddDays(-2); // Expired 2 days ago
            await db.SaveChangesAsync();
        }

        var memberRequest = new AddWorkspaceMemberRequest(
            Username: "staff_" + Guid.NewGuid().ToString("N")[..6],
            Email: TestDataGenerator.GenerateUniqueEmail("staff"),
            Password: "Password123!",
            DisplayName: "Teacher",
            Phone: TestDataGenerator.GenerateEgyptianPhone(),
            Role: "Teacher"
        );

        // Act: Owner attempts mutating request on expired workspace
        var response = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/members", memberRequest, JsonSerializationHelper.Options);

        // Assert: Intercepted by SubscriptionLockoutMiddleware
        var problem = await response.ShouldHaveProblemDetails(HttpStatusCode.Forbidden, "SUBSCRIPTION_EXPIRED");
        problem.ShouldHaveSupportHotlines();
    }

    [Fact]
    public async Task ReadRequest_WhenLockedOut_PassesThroughSuccessfully()
    {
        // Arrange: Workspace suspended
        var (workspaceId, _, _, ownerClient) = await ProvisionWorkspaceAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        await adminClient.PostAsJsonAsync($"/api/admin/workspaces/{workspaceId}/suspend", new { reason = "Hold" }, JsonSerializationHelper.Options);

        // Act: Owner sends read request (GET /members)
        var response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/members");

        // Assert: Read passes through unblocked
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhitelistedEndpoints_WhenLockedOut_PassThroughSuccessfully()
    {
        // Arrange: Workspace suspended
        var (workspaceId, _, _, ownerClient) = await ProvisionWorkspaceAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        await adminClient.PostAsJsonAsync($"/api/admin/workspaces/{workspaceId}/suspend", new { reason = "Hold" }, JsonSerializationHelper.Options);

        // Act: Query subscription status (whitelisted endpoint)
        var response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/subscription-status");

        // Assert: Returns 200 OK with lockout details
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<WorkspaceSubscriptionStatusDto>(JsonSerializationHelper.Options);
        status.Should().NotBeNull();
        status!.IsLockedOut.Should().BeTrue();
        status.IsSuspended.Should().BeTrue();
        status.SupportPhone1.Should().Be("01272785618");
        status.SupportPhone2.Should().Be("01019482687");
    }

    [Fact]
    public async Task Request_WithMismatchedRouteAndHeaderWorkspaceId_Returns400WorkspaceIdMismatch()
    {
        // Arrange: Route has idA, Header has idB
        var (workspaceId, _, _, ownerClient) = await ProvisionWorkspaceAsync();
        var differentWorkspaceId = Guid.NewGuid();

        // Create client with conflicting header
        ownerClient.DefaultRequestHeaders.Remove("X-Workspace-Id");
        ownerClient.DefaultRequestHeaders.Add("X-Workspace-Id", differentWorkspaceId.ToString());

        // Act: Send request where route ID != header ID
        var response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/members");

        // Assert: Intercepted by anti-spoofing check
        var problem = await response.ShouldHaveProblemDetails(HttpStatusCode.BadRequest, "WORKSPACE_ID_MISMATCH");
        problem.Detail.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Request_AccessingAnotherTenantWorkspace_Returns403Forbidden()
    {
        // Arrange: Provision Workspace A (Owner A) and Workspace B
        var (workspaceAId, _, _, ownerAClient) = await ProvisionWorkspaceAsync();
        var (workspaceBId, _, _, _) = await ProvisionWorkspaceAsync();

        // Owner A attempts to access Workspace B using Workspace B's header
        ownerAClient.DefaultRequestHeaders.Remove("X-Workspace-Id");
        ownerAClient.DefaultRequestHeaders.Add("X-Workspace-Id", workspaceBId.ToString());

        // Act: Owner A queries Workspace B members
        var response = await ownerAClient.GetAsync($"/api/workspaces/{workspaceBId}/members");

        // Assert: Tenant authorization rejects with 403 Forbidden
        var problem = await response.ShouldHaveProblemDetails(HttpStatusCode.Forbidden, "UNAUTHORIZED_WORKSPACE_ACCESS");
        problem.Detail.Should().NotBeNullOrWhiteSpace();
    }
}
