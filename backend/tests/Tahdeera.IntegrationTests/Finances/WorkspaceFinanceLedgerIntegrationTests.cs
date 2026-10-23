using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Finances;

[Collection("IntegrationTests")]
public class WorkspaceFinanceLedgerIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public WorkspaceFinanceLedgerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid WorkspaceId, HttpClient OwnerClient, Guid OwnerUserId)> SetupWorkspaceAsync()
    {
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "PROFESSIONAL_CENTER");

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("ledger_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("LedgerCenter"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Ledger Owner",
                Phone: TestDataGenerator.GenerateEgyptianPhone()),
            Subscription: new CreateInitialSubscriptionDto(
                PlanId: plan.Id,
                StartsAt: DateTimeOffset.UtcNow,
                EndsAt: DateTimeOffset.UtcNow.AddDays(365),
                AgreedPrice: plan.DefaultPrice,
                InitialPayment: new CreateInitialPaymentDto(plan.DefaultPrice, "Cash", null, null))
        );

        var wsResponse = await adminClient.PostAsJsonAsync("/api/admin/workspaces", createWsCommand, JsonSerializationHelper.Options);
        wsResponse.EnsureSuccessStatusCode();
        var wsResult = await wsResponse.Content.ReadFromJsonAsync<CreateWorkspaceResultDto>(JsonSerializationHelper.Options);

        var ownerClient = await _factory.CreateAuthenticatedClientAsync(ownerEmail, ownerPassword);
        return (wsResult!.WorkspaceId, ownerClient, wsResult.OwnerUserId);
    }

    [Fact]
    public async Task GetLedger_ReturnsUnifiedChronologicalTransactionsAndPagination()
    {
        // Arrange
        var (workspaceId, ownerClient, ownerUserId) = await SetupWorkspaceAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

            var student = new Student
            {
                Id = Guid.NewGuid(),
                StudentCode = "LED-001",
                FullName = "طالب السجل المالي",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Students.Add(student);

            var wsStudent = new WorkspaceStudent
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                StudentId = student.Id,
                Status = WorkspaceStudentStatus.Active,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.WorkspaceStudents.Add(wsStudent);

            var account = StudentAccount.Create(wsStudent.Id, ownerUserId);
            db.StudentAccounts.Add(account);

            // Charge 1 (older)
            var charge = StudentCharge.Create(account.Id, "October Fee", 400m, createdByUserId: ownerUserId);
            db.StudentCharges.Add(charge);

            // Payment 1 (newer)
            var payment = StudentPayment.Create(
                account.Id,
                "REC-LED-001",
                400m,
                PaymentMethod.Cash,
                ownerUserId,
                paidAt: DateTimeOffset.UtcNow.AddMinutes(5));
            db.StudentPayments.Add(payment);

            await db.SaveChangesAsync();
        }

        // Act 1: Get ledger
        var response = await ownerClient.GetAsync($"/api/v1/workspaces/{workspaceId}/finance/ledger?pageIndex=0&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkspaceFinanceLedgerItemDto>>(JsonSerializationHelper.Options);
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);

        // Verification of ordering: Payment first, Charge second
        result.Items[0].Id.Should().Be("REC-LED-001");
        result.Items[0].Type.Should().Be("Payment");
        result.Items[0].Amount.Should().Be(400m);
        result.Items[0].StudentCode.Should().Be("LED-001");

        result.Items[1].Type.Should().Be("Charge");
        result.Items[1].Amount.Should().Be(400m);

        // Act 2: Filter by transactionType=Charge
        var chargeResponse = await ownerClient.GetAsync($"/api/v1/workspaces/{workspaceId}/finance/ledger?transactionType=Charge");
        chargeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var chargeResult = await chargeResponse.Content.ReadFromJsonAsync<PagedResult<WorkspaceFinanceLedgerItemDto>>(JsonSerializationHelper.Options);
        chargeResult!.TotalCount.Should().Be(1);
        chargeResult.Items[0].Type.Should().Be("Charge");

        // Act 3: RBAC check
        var anonClient = _factory.CreateClient();
        var anonResponse = await anonClient.GetAsync($"/api/v1/workspaces/{workspaceId}/finance/ledger");
        anonResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
