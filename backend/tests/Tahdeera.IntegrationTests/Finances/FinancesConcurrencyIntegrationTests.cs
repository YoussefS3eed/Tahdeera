using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.API.Contracts.Requests;
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
public class FinancesConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public FinancesConcurrencyIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ConcurrentPayments_OnSingleOpenCharge_PessimisticLockPreventsOverAllocation()
    {
        // 1. Arrange: Setup workspace and student
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "TEACHER_STARTER");

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("concur_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("ConcurCenter"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Concur Owner",
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

        var student = new Student
        {
            Id = Guid.NewGuid(),
            FullName = "Sara Concurrency",
            StudentCode = "STU-" + Guid.NewGuid().ToString("N")[..6],
            IsActive = true
        };
        var wsStudent = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsResult!.WorkspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active
        };

        db.Students.Add(student);
        db.WorkspaceStudents.Add(wsStudent);
        await db.SaveChangesAsync();

        var client = await _factory.CreateAuthenticatedClientAsync(ownerEmail, ownerPassword);
        client.DefaultRequestHeaders.Add("X-Workspace-Id", wsResult.WorkspaceId.ToString());

        // 2. Post a single charge of 200.00 EGP
        var chargeRequest = new CreateStudentChargeRequest(
            GroupId: null,
            BillingPeriodLabel: "2026-10",
            Description: "Course Fee",
            Amount: 200.00m,
            DueAt: DateTimeOffset.UtcNow.AddDays(5),
            Notes: null);

        var chargeResponse = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{wsResult.WorkspaceId}/students/{student.Id}/finance/charges",
            chargeRequest,
            JsonSerializationHelper.Options);
        chargeResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 3. Act: Fire 6 concurrent payments of 50.00 EGP each (Total = 300 EGP against 200 EGP charge)
        const int concurrentCount = 6;
        var tasks = Enumerable.Range(0, concurrentCount).Select(async i =>
        {
            var taskClient = await _factory.CreateAuthenticatedClientAsync(ownerEmail, ownerPassword);
            taskClient.DefaultRequestHeaders.Add("X-Workspace-Id", wsResult.WorkspaceId.ToString());

            var paymentRequest = new RecordStudentPaymentRequest(
                Amount: 50.00m,
                Method: PaymentMethod.Cash,
                PaidAt: DateTimeOffset.UtcNow,
                Reference: $"CONCUR-{i}",
                Notes: null,
                Allocations: null);

            return await taskClient.PostAsJsonAsync(
                $"/api/v1/workspaces/{wsResult.WorkspaceId}/students/{student.Id}/finance/payments",
                paymentRequest,
                JsonSerializationHelper.Options);
        }).ToArray();

        var responses = await Task.WhenAll(tasks);

        // 4. Assert: All 6 payments succeeded (201 Created)
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Created);

        // 5. Query final statement
        var statementResponse = await client.GetAsync($"/api/v1/workspaces/{wsResult.WorkspaceId}/students/{student.Id}/finance/account");
        statementResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var statement = await statementResponse.Content.ReadFromJsonAsync<StudentAccountStatementDto>(JsonSerializationHelper.Options);

        // Total Open Charges = 200m
        // Total Lifetime Payments = 300m (6 x 50m)
        // Current Balance = 200m - 300m = -100m (Overpaid by 100m)
        // Total Unallocated Credit = 100m
        statement!.TotalOpenCharges.Should().Be(200.00m);
        statement.TotalLifetimePayments.Should().Be(300.00m);
        statement.CurrentBalance.Should().Be(-100.00m);
        statement.BalanceStatus.Should().Be("Overpaid");
        statement.TotalUnallocatedCredit.Should().Be(100.00m);
    }
}
