using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.ValueObjects;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Finances;

[Collection("IntegrationTests")]
public class FinanceExportIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public FinanceExportIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExportFinanceCsv_StreamsUtf8BomAndCorrectTransactions()
    {
        // 1. Setup Workspace and Plan with ExcelExport enabled
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "TEACHER_STARTER");
        plan.Features = new SubscriptionPlanFeatures(ExcelExport: true);
        await db.SaveChangesAsync();

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("export_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("ExportCenter"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Export Owner",
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
        var workspaceId = wsResult!.WorkspaceId;

        var ownerClient = await _factory.CreateAuthenticatedClientAsync(ownerEmail, ownerPassword);
        ownerClient.DefaultRequestHeaders.Add("X-Workspace-Id", workspaceId.ToString());

        // 2. Setup Student and Account with Charge & Payment
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FullName = "Mahmoud Hassan",
            StudentCode = $"STU-{Guid.NewGuid().ToString("N")[..6]}",
            IsActive = true
        };
        var wsStudent = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active,
            SeatNumber = "C-11"
        };
        var account = StudentAccount.Create(wsStudent.Id);
        var charge = StudentCharge.Create(account.Id, "November Exam Fee", 150.00m, billingPeriodLabel: "2026-11");

        var ownerUser = await db.AppUsers.FirstAsync(u => u.NormalizedEmail == ownerEmail.ToUpperInvariant());
        var payment = StudentPayment.Create(
            account.Id,
            "REC-202611-0077",
            150.00m,
            PaymentMethod.Cash,
            ownerUser.Id,
            paidAt: DateTimeOffset.UtcNow,
            reference: "RECEIPT-REF-01");

        db.Students.Add(student);
        db.WorkspaceStudents.Add(wsStudent);
        db.StudentAccounts.Add(account);
        db.StudentCharges.Add(charge);
        db.StudentPayments.Add(payment);
        await db.SaveChangesAsync();

        // 3. Request Export Endpoint
        var fromDate = DateTimeOffset.UtcNow.AddDays(-5).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var toDate = DateTimeOffset.UtcNow.AddDays(5).ToString("yyyy-MM-ddTHH:mm:ssZ");

        var exportResponse = await ownerClient.GetAsync(
            $"/api/v1/workspaces/{workspaceId}/finance/export?fromDate={fromDate}&toDate={toDate}");

        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        exportResponse.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");

        var bytes = await exportResponse.Content.ReadAsByteArrayAsync();

        // Assert UTF-8 BOM bytes 0xEF, 0xBB, 0xBF at start
        bytes.Length.Should().BeGreaterThan(3);
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);

        var csvText = System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        csvText.Should().Contain("Date (UTC),Date (Local),Receipt/Ref");
        csvText.Should().Contain("Mahmoud Hassan");
        csvText.Should().Contain("REC-202611-0077");
        csvText.Should().Contain("November Exam Fee");
        csvText.Should().Contain("150.00");
    }
}
