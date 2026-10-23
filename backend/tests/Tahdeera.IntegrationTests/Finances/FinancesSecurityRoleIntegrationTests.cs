using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Finances;

[Collection("IntegrationTests")]
public class FinancesSecurityRoleIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public FinancesSecurityRoleIntegrationTests(CustomWebApplicationFactory factory)
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
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "PROFESSIONAL_CENTER");

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("sec_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("SecCenter"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Security Owner",
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
            FullName = "Sec Student",
            StudentCode = "STU-" + Guid.NewGuid().ToString("N")[..6],
            IsActive = true
        };
        var wsStudent = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsResult.WorkspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active
        };

        db.Students.Add(student);
        db.WorkspaceStudents.Add(wsStudent);
        await db.SaveChangesAsync();

        return (wsResult.WorkspaceId, student.Id, ownerClient);
    }

    private async Task<HttpClient> CreateStaffMemberClientAsync(Guid workspaceId, HttpClient ownerClient, string role)
    {
        var staffEmail = TestDataGenerator.GenerateUniqueEmail(role.ToLowerInvariant());
        var staffPassword = "StaffPassword123!";
        var staffPhone = TestDataGenerator.GenerateEgyptianPhone();
        var staffUsername = role.ToLowerInvariant()[..3] + "_" + Guid.NewGuid().ToString("N")[..6];

        var request = new AddWorkspaceMemberRequest(
            Username: staffUsername,
            Email: staffEmail,
            Password: staffPassword,
            DisplayName: $"{role} Staff",
            Phone: staffPhone,
            Role: role);

        var response = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/members", request, JsonSerializationHelper.Options);
        response.EnsureSuccessStatusCode();

        var client = await _factory.CreateAuthenticatedClientAsync(staffEmail, staffPassword);
        client.DefaultRequestHeaders.Add("X-Workspace-Id", workspaceId.ToString());
        return client;
    }

    [Fact]
    public async Task RolePermissions_AccountantAllowed_TeacherAndAssistantAndReceptionForbidden()
    {
        // Arrange
        var (workspaceId, studentId, ownerClient) = await SetupWorkspaceAndStudentAsync();

        var accountantClient = await CreateStaffMemberClientAsync(workspaceId, ownerClient, "Accountant");
        var teacherClient = await CreateStaffMemberClientAsync(workspaceId, ownerClient, "Teacher");
        var assistantClient = await CreateStaffMemberClientAsync(workspaceId, ownerClient, "Assistant");
        var receptionClient = await CreateStaffMemberClientAsync(workspaceId, ownerClient, "Reception");

        // Act & Assert 1: Accountant can view statement (200 OK)
        var accStatementResp = await accountantClient.GetAsync($"/api/v1/workspaces/{workspaceId}/students/{studentId}/finance/account");
        accStatementResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act & Assert 2: Accountant can create charge (201 Created)
        var chargeReq = new CreateStudentChargeRequest(null, "2026-10", "Tuition Fee", 300m, null, null);
        var accChargeResp = await accountantClient.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspaceId}/students/{studentId}/finance/charges",
            chargeReq,
            JsonSerializationHelper.Options);
        accChargeResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act & Assert 3: Teacher cannot view statement (403 Forbidden)
        var teacherStatementResp = await teacherClient.GetAsync($"/api/v1/workspaces/{workspaceId}/students/{studentId}/finance/account");
        teacherStatementResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act & Assert 4: Teacher cannot create charge (403 Forbidden)
        var teacherChargeResp = await teacherClient.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspaceId}/students/{studentId}/finance/charges",
            chargeReq,
            JsonSerializationHelper.Options);
        teacherChargeResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act & Assert 5: Assistant cannot view statement (403 Forbidden)
        var assistantResp = await assistantClient.GetAsync($"/api/v1/workspaces/{workspaceId}/students/{studentId}/finance/account");
        assistantResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act & Assert 6: Reception cannot view statement (403 Forbidden)
        var receptionResp = await receptionClient.GetAsync($"/api/v1/workspaces/{workspaceId}/students/{studentId}/finance/account");
        receptionResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
