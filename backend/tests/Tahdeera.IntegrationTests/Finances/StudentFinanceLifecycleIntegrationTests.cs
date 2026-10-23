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
public class StudentFinanceLifecycleIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public StudentFinanceLifecycleIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid WorkspaceId, Guid StudentId, HttpClient OwnerClient)> SetupWorkspaceAndStudentAsync()
    {
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "TEACHER_STARTER");

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("fin_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("FinanceCenter"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Finance Owner",
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
        ownerClient.DefaultRequestHeaders.Add("X-Workspace-Id", wsResult!.WorkspaceId.ToString());

        var student = new Student
        {
            Id = Guid.NewGuid(),
            FullName = "Tarek Hany",
            StudentCode = "STU-" + Guid.NewGuid().ToString("N")[..6],
            IsActive = true
        };
        var wsStudent = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsResult.WorkspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active,
            GuardianName = "Hany Ali",
            GuardianPhone = "+201012345678",
            WhatsappOptIn = true
        };

        db.Students.Add(student);
        db.WorkspaceStudents.Add(wsStudent);
        await db.SaveChangesAsync();

        return (wsResult.WorkspaceId, student.Id, ownerClient);
    }

    [Fact]
    public async Task FullFinancialLifecycle_Charge_Payment_Statement_Reversal_Success()
    {
        // 1. Arrange: Setup workspace and student
        var (workspaceId, studentId, client) = await SetupWorkspaceAndStudentAsync();

        // 2. Act: Post Charge (400 EGP)
        var chargeRequest = new CreateStudentChargeRequest(
            GroupId: null,
            BillingPeriodLabel: "2026-10",
            Description: "October 2026 Monthly Tuition",
            Amount: 400.00m,
            DueAt: DateTimeOffset.UtcNow.AddDays(7),
            Notes: "Includes 4 sessions");

        var chargeResponse = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspaceId}/students/{studentId}/finance/charges",
            chargeRequest,
            JsonSerializationHelper.Options);

        chargeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var chargeDto = await chargeResponse.Content.ReadFromJsonAsync<StudentChargeDto>(JsonSerializationHelper.Options);
        chargeDto.Should().NotBeNull();
        chargeDto!.Amount.Should().Be(400.00m);
        chargeDto.RemainingAmount.Should().Be(400.00m);

        // 3. Query Statement -> InArrears (400 EGP)
        var statement1Response = await client.GetAsync($"/api/v1/workspaces/{workspaceId}/students/{studentId}/finance/account");
        statement1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var statement1 = await statement1Response.Content.ReadFromJsonAsync<StudentAccountStatementDto>(JsonSerializationHelper.Options);
        statement1!.CurrentBalance.Should().Be(400.00m);
        statement1.BalanceStatus.Should().Be("InArrears");

        // 4. Act: Ingest Payment (400 EGP Cash) with FIFO
        var paymentRequest = new RecordStudentPaymentRequest(
            Amount: 400.00m,
            Method: PaymentMethod.Cash,
            PaidAt: DateTimeOffset.UtcNow,
            Reference: "CASH-001",
            Notes: "Front desk payment",
            Allocations: null);

        var paymentResponse = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspaceId}/students/{studentId}/finance/payments",
            paymentRequest,
            JsonSerializationHelper.Options);

        paymentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var paymentDto = await paymentResponse.Content.ReadFromJsonAsync<RecordPaymentResultDto>(JsonSerializationHelper.Options);
        paymentDto.Should().NotBeNull();
        paymentDto!.AllocatedAmount.Should().Be(400.00m);
        paymentDto.UnallocatedCredit.Should().Be(0.00m);
        paymentDto.WhatsAppReceiptStaged.Should().BeTrue();

        // 5. Query Statement -> Settled (0 EGP)
        var statement2Response = await client.GetAsync($"/api/v1/workspaces/{workspaceId}/students/{studentId}/finance/account");
        statement2Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var statement2 = await statement2Response.Content.ReadFromJsonAsync<StudentAccountStatementDto>(JsonSerializationHelper.Options);
        statement2!.CurrentBalance.Should().Be(0.00m);
        statement2.BalanceStatus.Should().Be("Settled");

        // 6. Act: Reverse Payment
        var reverseRequest = new ReverseStudentPaymentRequest("Bounced check or refund");
        var reverseResponse = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspaceId}/students/{studentId}/finance/payments/{paymentDto.PaymentId}/reverse",
            reverseRequest,
            JsonSerializationHelper.Options);

        reverseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reverseDto = await reverseResponse.Content.ReadFromJsonAsync<ReversePaymentResultDto>(JsonSerializationHelper.Options);
        reverseDto!.RestoredArrearsAmount.Should().Be(400.00m);
        reverseDto.ReopenedChargesCount.Should().Be(1);

        // 7. Query Statement -> InArrears Restored (400 EGP)
        var statement3Response = await client.GetAsync($"/api/v1/workspaces/{workspaceId}/students/{studentId}/finance/account");
        statement3Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var statement3 = await statement3Response.Content.ReadFromJsonAsync<StudentAccountStatementDto>(JsonSerializationHelper.Options);
        statement3!.CurrentBalance.Should().Be(400.00m);
        statement3.BalanceStatus.Should().Be("InArrears");
    }
}
