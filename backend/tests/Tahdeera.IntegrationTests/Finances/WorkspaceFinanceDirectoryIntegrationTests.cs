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
public class WorkspaceFinanceDirectoryIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public WorkspaceFinanceDirectoryIntegrationTests(CustomWebApplicationFactory factory)
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

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("dir_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("DirectoryCenter"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Directory Owner",
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
    public async Task GetStudents_ReturnsPaginatedDirectoryAndFilteredStatus()
    {
        // Arrange
        var (workspaceId, ownerClient, ownerUserId) = await SetupWorkspaceAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

            // Seed 25 students:
            // 1-10: Unpaid (has 500 charge, 0 payments)
            // 11-20: Paid (has 500 charge, 500 payment)
            // 21-25: Settled (0 charges)
            for (int i = 1; i <= 25; i++)
            {
                var student = new Student
                {
                    Id = Guid.NewGuid(),
                    StudentCode = $"DIR-{i:D3}",
                    FullName = $"طالب اختبار {i:D3}",
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

                if (i <= 10)
                {
                    // Unpaid
                    var charge = StudentCharge.Create(account.Id, "Monthly Tuition", 500m, createdByUserId: ownerUserId);
                    db.StudentCharges.Add(charge);
                }
                else if (i <= 20)
                {
                    // Paid
                    var charge = StudentCharge.Create(account.Id, "Monthly Tuition", 500m, createdByUserId: ownerUserId);
                    db.StudentCharges.Add(charge);
                    var payment = StudentPayment.Create(account.Id, $"REC-{i:D3}", 500m, PaymentMethod.Cash, ownerUserId);
                    db.StudentPayments.Add(payment);
                }
                // 21-25: Settled (no charges)
            }

            await db.SaveChangesAsync();
        }

        // Act 1: Get page 0 with size 10
        var page0Response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/finance/students?pageIndex=0&pageSize=10");
        page0Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page0Result = await page0Response.Content.ReadFromJsonAsync<PagedResult<WorkspaceFinanceStudentItemDto>>(JsonSerializationHelper.Options);
        page0Result.Should().NotBeNull();
        page0Result!.TotalCount.Should().Be(25);
        page0Result.Items.Should().HaveCount(10);
        page0Result.TotalPages.Should().Be(3);
        page0Result.HasNextPage.Should().BeTrue();
        page0Result.HasPreviousPage.Should().BeFalse();

        // Act 2: Filter by status=Unpaid
        var unpaidResponse = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/finance/students?status=Unpaid&pageSize=50");
        unpaidResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var unpaidResult = await unpaidResponse.Content.ReadFromJsonAsync<PagedResult<WorkspaceFinanceStudentItemDto>>(JsonSerializationHelper.Options);
        unpaidResult.Should().NotBeNull();
        unpaidResult!.TotalCount.Should().Be(10);
        unpaidResult.Items.Should().HaveCount(10);
        unpaidResult.Items.Should().OnlyContain(item => item.PaymentStatus == "Unpaid");

        // Act 3: Filter by status=Paid
        var paidResponse = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/finance/students?status=Paid&pageSize=50");
        paidResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var paidResult = await paidResponse.Content.ReadFromJsonAsync<PagedResult<WorkspaceFinanceStudentItemDto>>(JsonSerializationHelper.Options);
        paidResult.Should().NotBeNull();
        paidResult!.TotalCount.Should().Be(10);
        paidResult.Items.Should().OnlyContain(item => item.PaymentStatus == "Paid");

        // Act 4: Filter by status=Settled
        var settledResponse = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/finance/students?status=Settled&pageSize=50");
        settledResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var settledResult = await settledResponse.Content.ReadFromJsonAsync<PagedResult<WorkspaceFinanceStudentItemDto>>(JsonSerializationHelper.Options);
        settledResult.Should().NotBeNull();
        settledResult!.TotalCount.Should().Be(5);
        settledResult.Items.Should().OnlyContain(item => item.PaymentStatus == "Settled");

        // Act 5: RBAC - Unauthenticated client
        var anonClient = _factory.CreateClient();
        var anonResponse = await anonClient.GetAsync($"/api/workspaces/{workspaceId}/finance/students");
        anonResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
