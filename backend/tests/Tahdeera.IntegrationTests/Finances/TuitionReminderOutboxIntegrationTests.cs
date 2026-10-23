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
public class TuitionReminderOutboxIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public TuitionReminderOutboxIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task StageReminders_DebtorWithOptInAndValidPhone_StagesOutboxAndAppliesCooldown()
    {
        // 1. Setup Workspace and Owner
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "TEACHER_STARTER");

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("remind_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("ReminderCenter"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Reminder Owner",
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

        // 2. Setup Debtor Student with WhatsApp opt-in
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FullName = "Khaled Mostafa",
            StudentCode = $"STU-{Guid.NewGuid().ToString("N")[..6]}",
            IsActive = true
        };
        var wsStudent = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active,
            GuardianPhone = "01012345678",
            WhatsappOptIn = true
        };
        var account = StudentAccount.Create(wsStudent.Id);
        var charge = StudentCharge.Create(account.Id, "October Fee", 500.00m);

        db.Students.Add(student);
        db.WorkspaceStudents.Add(wsStudent);
        db.StudentAccounts.Add(account);
        db.StudentCharges.Add(charge);
        await db.SaveChangesAsync();

        // 3. First Reminder Trigger
        var reminderReq = new StageTuitionRemindersRequest(
            GroupId: null,
            MinimumBalanceThreshold: 50.00m);

        var trigger1Response = await ownerClient.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspaceId}/finance/reminders",
            reminderReq,
            JsonSerializationHelper.Options);

        trigger1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var trigger1Result = await trigger1Response.Content.ReadFromJsonAsync<StageTuitionRemindersResultDto>(JsonSerializationHelper.Options);

        trigger1Result.Should().NotBeNull();
        trigger1Result!.TotalDebtorsFound.Should().Be(1);
        trigger1Result.RemindersStagedCount.Should().Be(1);
        trigger1Result.SkippedCooldownCount.Should().Be(0);

        // Verify outbox row in real database
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var outboxRow = await verifyDb.MessageOutboxes
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.StudentId == student.Id);

        outboxRow.Should().NotBeNull();
        outboxRow!.MessageType.Should().Be("TuitionReminder");
        outboxRow.TemplateName.Should().Be("tuition_reminder_v1");
        outboxRow.RecipientPhone.Should().Be("01012345678");

        // 4. Second Reminder Trigger Immediately -> Cooldown applies
        var trigger2Response = await ownerClient.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspaceId}/finance/reminders",
            reminderReq,
            JsonSerializationHelper.Options);

        trigger2Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var trigger2Result = await trigger2Response.Content.ReadFromJsonAsync<StageTuitionRemindersResultDto>(JsonSerializationHelper.Options);

        trigger2Result.Should().NotBeNull();
        trigger2Result!.TotalDebtorsFound.Should().Be(1);
        trigger2Result.RemindersStagedCount.Should().Be(0);
        trigger2Result.SkippedCooldownCount.Should().Be(1);
    }
}
