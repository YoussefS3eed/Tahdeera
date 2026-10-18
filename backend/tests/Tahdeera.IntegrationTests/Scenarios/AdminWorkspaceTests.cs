using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class AdminWorkspaceTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminWorkspaceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> GetPlanIdAsync(string code = "TEACHER_STARTER")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == code);
        return plan.Id;
    }

    [Fact]
    public async Task ProvisionWorkspace_WithValidData_CreatesFullDatabaseGraphAndAuditLog()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        var planId = await GetPlanIdAsync("TEACHER_STARTER");
        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("owner");
        var workspaceName = TestDataGenerator.GenerateWorkspaceName("Alpha");

        var command = new CreateWorkspaceCommand(
            Name: workspaceName,
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo, Egypt",
            DefaultLateGraceMinutes: 15,
            Owner: new CreateOwnerDto(
                Username: "owner_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: "OwnerPassword123!",
                DisplayName: "Alpha Owner",
                Phone: TestDataGenerator.GenerateEgyptianPhone()
            ),
            Subscription: new CreateInitialSubscriptionDto(
                PlanId: planId,
                StartsAt: DateTimeOffset.UtcNow,
                EndsAt: DateTimeOffset.UtcNow.AddDays(365),
                AgreedPrice: 500.00m,
                InitialPayment: new CreateInitialPaymentDto(
                    Amount: 500.00m,
                    Method: "Cash",
                    Reference: "RCPT-001",
                    Notes: "Initial activation payment"
                )
            )
        );

        // Act: Provision workspace
        var response = await adminClient.PostAsJsonAsync("/api/admin/workspaces", command, JsonSerializationHelper.Options);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateWorkspaceResultDto>(JsonSerializationHelper.Options);
        result.Should().NotBeNull();
        result!.WorkspaceId.Should().NotBeEmpty();
        result.OwnerUserId.Should().NotBeEmpty();
        result.SubscriptionId.Should().NotBeEmpty();
        result.PaymentId.Should().NotBeEmpty();

        // Assert PostgreSQL Database Entities
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        // 1. Workspace
        var wsInDb = await db.Workspaces.FindAsync(result.WorkspaceId);
        wsInDb.Should().NotBeNull();
        wsInDb!.Name.Should().Be(workspaceName);
        wsInDb.IsActive.Should().BeTrue();

        // 2. Owner User
        var ownerInDb = await db.AppUsers.FindAsync(result.OwnerUserId);
        ownerInDb.Should().NotBeNull();
        ownerInDb!.NormalizedEmail.Should().Be(ownerEmail.ToUpperInvariant());

        // 3. Workspace Member
        var memberInDb = await db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == result.WorkspaceId && m.UserId == result.OwnerUserId);
        memberInDb.Should().NotBeNull();
        memberInDb!.Role.Should().Be("Owner");
        memberInDb.IsActive.Should().BeTrue();

        // 4. Workspace Subscription
        var subInDb = await db.WorkspaceSubscriptions.FindAsync(result.SubscriptionId);
        subInDb.Should().NotBeNull();
        subInDb!.Status.Should().Be("Active");
        subInDb.PlanId.Should().Be(planId);

        // 5. Subscription Payment
        var paymentInDb = await db.SubscriptionPayments.FindAsync(result.PaymentId);
        paymentInDb.Should().NotBeNull();
        paymentInDb!.Amount.Should().Be(500.00m);
        paymentInDb.Status.Should().Be("Received");

        // 6. Audit Log
        var auditInDb = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.WorkspaceId == result.WorkspaceId && a.Action == "WORKSPACE_PROVISIONED");
        auditInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWorkspaces_WithPaginationAndSearch_ReturnsFilteredResults()
    {
        // Arrange: Provision a workspace with unique name
        var adminClient = await _factory.CreateAdminClientAsync();
        var planId = await GetPlanIdAsync("TEACHER_STARTER");
        var uniqueName = "SearchableCenter_" + Guid.NewGuid().ToString("N")[..6];

        var command = new CreateWorkspaceCommand(
            Name: uniqueName,
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Alexandria",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "srch_" + Guid.NewGuid().ToString("N")[..6],
                Email: TestDataGenerator.GenerateUniqueEmail("search"),
                Password: "Password123!",
                DisplayName: "Search Owner",
                Phone: TestDataGenerator.GenerateEgyptianPhone()
            ),
            Subscription: new CreateInitialSubscriptionDto(
                PlanId: planId,
                StartsAt: DateTimeOffset.UtcNow,
                EndsAt: DateTimeOffset.UtcNow.AddDays(365),
                AgreedPrice: 500.00m,
                InitialPayment: new CreateInitialPaymentDto(500.00m, "Cash", null, null)
            )
        );

        var provResponse = await adminClient.PostAsJsonAsync("/api/admin/workspaces", command, JsonSerializationHelper.Options);
        provResponse.EnsureSuccessStatusCode();

        // Act: Query workspaces with search filter
        var searchUrl = $"/api/admin/workspaces?page=1&pageSize=10&search={uniqueName}";
        var response = await adminClient.GetAsync(searchUrl);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await response.Content.ReadFromJsonAsync<PaginatedList<AdminWorkspaceListItemDto>>(JsonSerializationHelper.Options);
        pagedList.Should().NotBeNull();
        pagedList!.Items.Should().ContainSingle(w => w.Name == uniqueName);
    }

    [Fact]
    public async Task SuspendAndReactivateWorkspace_UpdatesStatusAndAuditTrail()
    {
        // Arrange: Provision workspace
        var adminClient = await _factory.CreateAdminClientAsync();
        var planId = await GetPlanIdAsync("TEACHER_STARTER");
        var command = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("SuspendTest"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Giza",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "susp_" + Guid.NewGuid().ToString("N")[..6],
                Email: TestDataGenerator.GenerateUniqueEmail("susp"),
                Password: "Password123!",
                DisplayName: "Susp Owner",
                Phone: TestDataGenerator.GenerateEgyptianPhone()
            ),
            Subscription: new CreateInitialSubscriptionDto(
                PlanId: planId,
                StartsAt: DateTimeOffset.UtcNow,
                EndsAt: DateTimeOffset.UtcNow.AddDays(365),
                AgreedPrice: 500.00m,
                InitialPayment: new CreateInitialPaymentDto(500.00m, "Cash", null, null)
            )
        );

        var provResponse = await adminClient.PostAsJsonAsync("/api/admin/workspaces", command, JsonSerializationHelper.Options);
        var provResult = await provResponse.Content.ReadFromJsonAsync<CreateWorkspaceResultDto>(JsonSerializationHelper.Options);
        var workspaceId = provResult!.WorkspaceId;

        // Act 1: Suspend workspace
        var suspendPayload = new { reason = "Suspicious financial activity" };
        var suspendResponse = await adminClient.PostAsJsonAsync($"/api/admin/workspaces/{workspaceId}/suspend", suspendPayload, JsonSerializationHelper.Options);
        suspendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert 1: Database reflects suspension
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
            var sub = await db.WorkspaceSubscriptions.FirstAsync(s => s.WorkspaceId == workspaceId);
            sub.ManualSuspendedAt.Should().NotBeNull();
            sub.SuspensionReason.Should().Be("Suspicious financial activity");

            var suspendAudit = await db.AuditLogs.FirstOrDefaultAsync(a => a.WorkspaceId == workspaceId && a.Action == "WORKSPACE_SUSPENDED");
            suspendAudit.Should().NotBeNull();
        }

        // Act 2: Reactivate workspace
        var reactivateResponse = await adminClient.PostAsync($"/api/admin/workspaces/{workspaceId}/reactivate", null);
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert 2: Database reflects reactivation
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
            var sub = await db.WorkspaceSubscriptions.FirstAsync(s => s.WorkspaceId == workspaceId);
            sub.ManualSuspendedAt.Should().BeNull();

            var reactivateAudit = await db.AuditLogs.FirstOrDefaultAsync(a => a.WorkspaceId == workspaceId && a.Action == "WORKSPACE_REACTIVATED");
            reactivateAudit.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task RenewWorkspaceSubscription_ExtendsDurationAndRecordsPayment()
    {
        // Arrange: Provision workspace
        var adminClient = await _factory.CreateAdminClientAsync();
        var planId = await GetPlanIdAsync("TEACHER_STARTER");
        var command = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("RenewTest"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "rnw_" + Guid.NewGuid().ToString("N")[..6],
                Email: TestDataGenerator.GenerateUniqueEmail("renew"),
                Password: "Password123!",
                DisplayName: "Renew Owner",
                Phone: TestDataGenerator.GenerateEgyptianPhone()
            ),
            Subscription: new CreateInitialSubscriptionDto(
                PlanId: planId,
                StartsAt: DateTimeOffset.UtcNow,
                EndsAt: DateTimeOffset.UtcNow.AddDays(30),
                AgreedPrice: 500.00m,
                InitialPayment: new CreateInitialPaymentDto(500.00m, "Cash", null, null)
            )
        );

        var provResponse = await adminClient.PostAsJsonAsync("/api/admin/workspaces", command, JsonSerializationHelper.Options);
        var provResult = await provResponse.Content.ReadFromJsonAsync<CreateWorkspaceResultDto>(JsonSerializationHelper.Options);
        var workspaceId = provResult!.WorkspaceId;

        // Act: Renew subscription for 60 days with 2000 EGP
        var renewPayload = new
        {
            amount = 2000.00m,
            method = "BankTransfer",
            reference = "TRF-9988",
            durationDays = 60
        };

        var renewResponse = await adminClient.PostAsJsonAsync($"/api/admin/workspaces/{workspaceId}/renew-subscription", renewPayload, JsonSerializationHelper.Options);

        // Assert HTTP response
        renewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var renewResult = await renewResponse.Content.ReadFromJsonAsync<RenewSubscriptionResultDto>(JsonSerializationHelper.Options);
        renewResult.Should().NotBeNull();
        renewResult!.EndsAt.Should().BeAfter(DateTimeOffset.UtcNow.AddDays(85));

        // Assert Database records in PostgreSQL
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var sub = await db.WorkspaceSubscriptions.FirstAsync(s => s.WorkspaceId == workspaceId);
        sub.EndsAt.Should().BeCloseTo(renewResult.EndsAt, TimeSpan.FromSeconds(5));

        var payment = await db.SubscriptionPayments.FirstOrDefaultAsync(p => p.SubscriptionId == sub.Id && p.Reference == "TRF-9988");
        payment.Should().NotBeNull();
        payment!.Amount.Should().Be(2000.00m);
        payment.Status.Should().Be("Received");

        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.WorkspaceId == workspaceId && a.Action == "SUBSCRIPTION_RENEWED");
        audit.Should().NotBeNull();
    }
}
