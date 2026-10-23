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
public class CentralCashierPaymentIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public CentralCashierPaymentIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid WorkspaceId, Guid StudentId, Guid ChargeId, HttpClient OwnerClient, Guid OwnerUserId)> SetupWorkspaceWithChargeAsync()
    {
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "PROFESSIONAL_CENTER");

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("cashier_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("CashierCenter"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Cashier Owner",
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

        var student = new Student
        {
            Id = Guid.NewGuid(),
            StudentCode = "CSH-001",
            FullName = "طالب شباك الدفع",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Students.Add(student);

        var wsStudent = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsResult!.WorkspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.WorkspaceStudents.Add(wsStudent);

        var account = StudentAccount.Create(wsStudent.Id, wsResult.OwnerUserId);
        db.StudentAccounts.Add(account);

        var charge = StudentCharge.Create(account.Id, "October Tuition", 450m, createdByUserId: wsResult.OwnerUserId);
        db.StudentCharges.Add(charge);

        await db.SaveChangesAsync();

        return (wsResult.WorkspaceId, student.Id, charge.Id, ownerClient, wsResult.OwnerUserId);
    }

    [Fact]
    public async Task RecordWorkspacePayment_ViaModalRoute_AllocatesAndReturns201Created()
    {
        // Arrange
        var (workspaceId, studentId, chargeId, ownerClient, ownerUserId) = await SetupWorkspaceWithChargeAsync();

        var request = new RecordWorkspacePaymentRequest(
            StudentId: studentId,
            Amount: 450.00m,
            PaymentMethod: "Cash",
            PaidAt: DateTimeOffset.UtcNow,
            TransactionReference: "TX-CENTRAL-01",
            Notes: "سداد عبر شباك الخزينة المركزي",
            Allocations: null);

        // Act
        var response = await ownerClient.PostAsJsonAsync($"/api/v1/workspaces/{workspaceId}/payments", request, JsonSerializationHelper.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<RecordPaymentResultDto>(JsonSerializationHelper.Options);
        result.Should().NotBeNull();
        result!.Amount.Should().Be(450.00m);
        result.ReceiptNumber.Should().StartWith("REC-");
        result.AllocatedAmount.Should().Be(450.00m);
        result.UnallocatedCredit.Should().Be(0.00m);
        result.Status.Should().Be("Received");

        // Verify database persistence
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var savedPayment = await db.StudentPayments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.ReceiptNumber == result.ReceiptNumber);

        savedPayment.Should().NotBeNull();
        savedPayment!.Amount.Should().Be(450.00m);
        savedPayment.Allocations.Should().HaveCount(1);
        savedPayment.Allocations.First().ChargeId.Should().Be(chargeId);
        savedPayment.Allocations.First().Amount.Should().Be(450.00m);
    }
}
