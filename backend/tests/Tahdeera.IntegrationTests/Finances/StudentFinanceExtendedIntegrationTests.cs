using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Finances;

[Collection("IntegrationTests")]
public class StudentFinanceExtendedIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public StudentFinanceExtendedIntegrationTests(CustomWebApplicationFactory factory)
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

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("ext_fin_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("ExtFinCenter"),
            Kind: "Teacher",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Finance Ext Owner",
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
            FullName = "عمر خالد الشافعي",
            StudentCode = "STU-" + Guid.NewGuid().ToString("N")[..6],
            IsActive = true
        };
        var wsStudent = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsResult.WorkspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active,
            GuardianName = "خالد الشافعي",
            GuardianPhone = "+201098765432",
            WhatsappOptIn = true
        };

        db.Students.Add(student);
        db.WorkspaceStudents.Add(wsStudent);
        await db.SaveChangesAsync();

        return (wsResult.WorkspaceId, student.Id, ownerClient);
    }

    [Fact]
    public async Task CancelStudentCharge_ValidCharge_CancelsAndRestoresBalanceToSettled()
    {
        // Arrange
        var (workspaceId, studentId, client) = await SetupWorkspaceAndStudentAsync();

        var chargeRequest = new CreateStudentChargeRequest(
            GroupId: null,
            BillingPeriodLabel: "2026-11",
            Description: "رسوم مذكرات دراسية",
            Amount: 150.00m,
            DueAt: DateTimeOffset.UtcNow.AddDays(5),
            Notes: "مذكرة الفصل الدراسي الأول");

        var chargeResponse = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/students/{studentId}/finance/charges",
            chargeRequest,
            JsonSerializationHelper.Options);

        chargeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var chargeDto = await chargeResponse.Content.ReadFromJsonAsync<StudentChargeDto>(JsonSerializationHelper.Options);

        // Verify balance is in arrears
        var statementBefore = await client.GetFromJsonAsync<StudentAccountStatementDto>(
            $"/api/workspaces/{workspaceId}/students/{studentId}/finance/account",
            JsonSerializationHelper.Options);
        statementBefore!.CurrentBalance.Should().Be(150.00m);
        statementBefore.BalanceStatus.Should().Be("InArrears");

        // Act: Cancel Charge
        var cancelRequest = new CancelStudentChargeRequest("إلغاء المذكرة بسبب تكرار القيد");
        var cancelResponse = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/students/{studentId}/finance/charges/{chargeDto!.Id}/cancel",
            cancelRequest,
            JsonSerializationHelper.Options);

        // Assert HTTP response
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelResult = await cancelResponse.Content.ReadFromJsonAsync<CancelChargeResultDto>(JsonSerializationHelper.Options);
        cancelResult.Should().NotBeNull();
        cancelResult!.ChargeId.Should().Be(chargeDto.Id);
        cancelResult.Status.Should().Be("Cancelled");

        // Assert Account Statement is restored to Settled
        var statementAfter = await client.GetFromJsonAsync<StudentAccountStatementDto>(
            $"/api/workspaces/{workspaceId}/students/{studentId}/finance/account",
            JsonSerializationHelper.Options);
        statementAfter!.CurrentBalance.Should().Be(0.00m);
        statementAfter.BalanceStatus.Should().Be("Settled");
    }

    [Fact]
    public async Task WriteOffStudentCharge_ValidCharge_WritesOffChargeAndRestoresSettled()
    {
        // Arrange
        var (workspaceId, studentId, client) = await SetupWorkspaceAndStudentAsync();

        var chargeRequest = new CreateStudentChargeRequest(
            GroupId: null,
            BillingPeriodLabel: "2026-11",
            Description: "رسوم اختبار تجريبي",
            Amount: 200.00m,
            DueAt: DateTimeOffset.UtcNow.AddDays(3),
            Notes: "اختبار نصف الفصل");

        var chargeResponse = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/students/{studentId}/finance/charges",
            chargeRequest,
            JsonSerializationHelper.Options);
        var chargeDto = await chargeResponse.Content.ReadFromJsonAsync<StudentChargeDto>(JsonSerializationHelper.Options);

        // Act: Write Off Charge
        var writeOffRequest = new WriteOffStudentChargeRequest("إعفاء الطالب من الرسوم لتفوقه الدراسي");
        var writeOffResponse = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/students/{studentId}/finance/charges/{chargeDto!.Id}/write-off",
            writeOffRequest,
            JsonSerializationHelper.Options);

        // Assert HTTP response
        writeOffResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var writeOffResult = await writeOffResponse.Content.ReadFromJsonAsync<WriteOffChargeResultDto>(JsonSerializationHelper.Options);
        writeOffResult.Should().NotBeNull();
        writeOffResult!.ChargeId.Should().Be(chargeDto.Id);
        writeOffResult.Status.Should().Be("WrittenOff");

        // Assert Account Statement
        var statement = await client.GetFromJsonAsync<StudentAccountStatementDto>(
            $"/api/workspaces/{workspaceId}/students/{studentId}/finance/account",
            JsonSerializationHelper.Options);
        statement!.CurrentBalance.Should().Be(0.00m);
        statement.BalanceStatus.Should().Be("Settled");
    }

    [Fact]
    public async Task GetChargesAndPaymentsLists_ReturnsOkWithPopulatedRecords()
    {
        // Arrange
        var (workspaceId, studentId, client) = await SetupWorkspaceAndStudentAsync();

        // 1. Create Charge
        var chargeRequest = new CreateStudentChargeRequest(
            GroupId: null,
            BillingPeriodLabel: "2026-10",
            Description: "رسوم شهر أكتوبر",
            Amount: 300.00m,
            DueAt: DateTimeOffset.UtcNow.AddDays(7),
            Notes: null);
        await client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/students/{studentId}/finance/charges", chargeRequest, JsonSerializationHelper.Options);

        // 2. Record Payment
        var paymentRequest = new RecordStudentPaymentRequest(
            Amount: 300.00m,
            Method: PaymentMethod.Cash,
            PaidAt: DateTimeOffset.UtcNow,
            Reference: "CASH-REC-01",
            Notes: "سداد كامل الرسوم",
            Allocations: null);
        await client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/students/{studentId}/finance/payments", paymentRequest, JsonSerializationHelper.Options);

        // Act: Query Charges
        var chargesResponse = await client.GetAsync($"/api/workspaces/{workspaceId}/students/{studentId}/finance/charges");
        chargesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var chargesWrapper = await chargesResponse.Content.ReadFromJsonAsync<ChargesResponseDto>(JsonSerializationHelper.Options);
        chargesWrapper.Should().NotBeNull();
        chargesWrapper!.Charges.Should().NotBeNull();
        chargesWrapper.Charges.Count.Should().Be(1);
        chargesWrapper.Charges[0].Amount.Should().Be(300.00m);

        // Act: Query Payments
        var paymentsResponse = await client.GetAsync($"/api/workspaces/{workspaceId}/students/{studentId}/finance/payments");
        paymentsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var paymentsWrapper = await paymentsResponse.Content.ReadFromJsonAsync<PaymentsResponseDto>(JsonSerializationHelper.Options);
        paymentsWrapper.Should().NotBeNull();
        paymentsWrapper!.Payments.Should().NotBeNull();
        paymentsWrapper.Payments.Count.Should().Be(1);
        paymentsWrapper.Payments[0].Amount.Should().Be(300.00m);
    }

    [Fact]
    public async Task GetPrintableReceipt_AfterPayment_ReturnsPrintableReceiptWithVerificationData()
    {
        // Arrange
        var (workspaceId, studentId, client) = await SetupWorkspaceAndStudentAsync();

        var chargeRequest = new CreateStudentChargeRequest(
            GroupId: null,
            BillingPeriodLabel: "2026-10",
            Description: "اشتراك شهري",
            Amount: 250.00m,
            DueAt: DateTimeOffset.UtcNow,
            Notes: null);
        await client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/students/{studentId}/finance/charges", chargeRequest, JsonSerializationHelper.Options);

        var paymentRequest = new RecordStudentPaymentRequest(
            Amount: 250.00m,
            Method: PaymentMethod.Wallet,
            PaidAt: DateTimeOffset.UtcNow,
            Reference: "VF-CASH-789",
            Notes: null,
            Allocations: null);
        var payResp = await client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/students/{studentId}/finance/payments", paymentRequest, JsonSerializationHelper.Options);
        var payResult = await payResp.Content.ReadFromJsonAsync<RecordPaymentResultDto>(JsonSerializationHelper.Options);

        // Act: Request Printable Receipt
        var receiptResponse = await client.GetAsync(
            $"/api/workspaces/{workspaceId}/students/{studentId}/finance/payments/{payResult!.PaymentId}/receipt-print");

        // Assert: 200 OK
        receiptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var receipt = await receiptResponse.Content.ReadFromJsonAsync<PrintableReceiptDto>(JsonSerializationHelper.Options);
        receipt.Should().NotBeNull();
        receipt!.PaymentId.Should().Be(payResult.PaymentId);
        receipt.Amount.Should().Be(250.00m);
        receipt.Currency.Should().Be("EGP");
        receipt.VerificationHash.Should().NotBeNullOrWhiteSpace();
        receipt.VerificationPath.Should().NotBeNullOrWhiteSpace();
        receipt.AmountInWordsArabic.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetStudent360Profile_ReturnsComprehensiveProfileData()
    {
        // Arrange
        var (workspaceId, studentId, client) = await SetupWorkspaceAndStudentAsync();

        // Act: Query 360 profile
        var profileResponse = await client.GetAsync($"/api/workspaces/{workspaceId}/students/{studentId}/360");

        // Assert: 200 OK
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await profileResponse.Content.ReadFromJsonAsync<Student360ProfileDto>(JsonSerializationHelper.Options);
        profile.Should().NotBeNull();
        profile!.StudentId.Should().Be(studentId);
        profile.FullName.Should().Be("عمر خالد الشافعي");
        profile.WorkspaceEnrollment.Should().NotBeNull();
        profile.WorkspaceEnrollment.GuardianName.Should().Be("خالد الشافعي");
        profile.FinancialSummary.Should().NotBeNull();
    }

    private sealed record ChargesResponseDto(List<StudentChargeDto> Charges);
    private sealed record PaymentsResponseDto(List<StudentPaymentDto> Payments);
}
