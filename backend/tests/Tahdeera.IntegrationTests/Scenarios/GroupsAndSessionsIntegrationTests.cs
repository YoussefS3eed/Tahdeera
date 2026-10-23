using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class GroupsAndSessionsIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public GroupsAndSessionsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid WorkspaceId, Guid AcademicYearId, short GradeId, Guid SubjectId, HttpClient OwnerClient, string OwnerEmail)> SetupWorkspaceAsync()
    {
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "TEACHER_STARTER");

        var academicYear = new AcademicYear
        {
            Id = Guid.NewGuid(),
            Name = "2026/2027",
            StartsOn = new DateOnly(2026, 9, 1),
            EndsOn = new DateOnly(2027, 6, 30),
            IsCurrent = true
        };
        db.AcademicYears.Add(academicYear);

        var grade = await db.Grades.FirstAsync();

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Code = "MATH10",
            NameAr = "رياضيات",
            NameEn = "Mathematics",
            IsActive = true
        };
        db.Subjects.Add(subject);
        await db.SaveChangesAsync();

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("grp_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("GroupSessionCenter"),
            Kind: "Teacher",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Mansoura, Egypt",
            DefaultLateGraceMinutes: 15,
            Owner: new CreateOwnerDto(
                Username: "grp_own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Group Center Owner",
                Phone: TestDataGenerator.GenerateEgyptianPhone()
            ),
            Subscription: new CreateInitialSubscriptionDto(
                PlanId: plan.Id,
                StartsAt: DateTimeOffset.UtcNow,
                EndsAt: DateTimeOffset.UtcNow.AddDays(365),
                AgreedPrice: plan.DefaultPrice,
                InitialPayment: new CreateInitialPaymentDto(plan.DefaultPrice, "Cash", null, null)
            )
        );

        var wsResponse = await adminClient.PostAsJsonAsync("/api/admin/workspaces", createWsCommand, JsonSerializationHelper.Options);
        wsResponse.EnsureSuccessStatusCode();
        var wsResult = await wsResponse.Content.ReadFromJsonAsync<CreateWorkspaceResultDto>(JsonSerializationHelper.Options);

        var ownerClient = await _factory.CreateAuthenticatedClientAsync(ownerEmail, ownerPassword);
        ownerClient.DefaultRequestHeaders.Add("X-Workspace-Id", wsResult!.WorkspaceId.ToString());

        return (wsResult.WorkspaceId, academicYear.Id, grade.Id, subject.Id, ownerClient, ownerEmail);
    }

    [Fact]
    public async Task CreateGroup_ValidRequest_CreatesGroupAndReturnsCreated()
    {
        // Arrange
        var (workspaceId, academicYearId, gradeId, subjectId, ownerClient, ownerEmail) = await SetupWorkspaceAsync();

        var request = new CreateGroupRequest(
            AcademicYearId: academicYearId,
            GradeId: gradeId,
            SubjectId: subjectId,
            Name: "مجموعة الرياضيات الثانوية - أ",
            RoomName: "قاعة 101",
            ScheduleSummary: "الأحد والثلاثاء 5:00 م",
            LateGraceMinutes: 20,
            DefaultMonthlyFee: 350.00m,
            ConsecutiveAbsenceThreshold: 3,
            AbsenceAction: AbsenceAction.NotifyOnly,
            TargetGender: "CoEd",
            MaxCapacity: 30
        );

        // Act
        var response = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/groups",
            request,
            JsonSerializationHelper.Options);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception($"HTTP Error: {response.StatusCode} -> {err}");
        }

        // Assert: 201 Created
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var groupDto = await response.Content.ReadFromJsonAsync<GroupDto>(JsonSerializationHelper.Options);
        groupDto.Should().NotBeNull();
        groupDto!.WorkspaceId.Should().Be(workspaceId);
        groupDto.Name.Should().Be("مجموعة الرياضيات الثانوية - أ");
        groupDto.DefaultMonthlyFee.Should().Be(350.00m);

        // Verify in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var dbGroup = await db.Groups.FindAsync(groupDto.Id);
        dbGroup.Should().NotBeNull();
        dbGroup!.Name.Should().Be("مجموعة الرياضيات الثانوية - أ");
        dbGroup.Status.Should().Be(GroupStatus.Active);
    }

    [Fact]
    public async Task GetGroupDetails_ReturnsOkWithDetails()
    {
        // Arrange
        var (workspaceId, academicYearId, gradeId, subjectId, ownerClient, _) = await SetupWorkspaceAsync();

        var createReq = new CreateGroupRequest(
            AcademicYearId: academicYearId,
            GradeId: gradeId,
            SubjectId: subjectId,
            Name: "مجموعة التفوق",
            RoomName: "قاعة 2",
            ScheduleSummary: "السبت 4:00 م",
            DefaultMonthlyFee: 400.00m
        );
        var createResp = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/groups", createReq, JsonSerializationHelper.Options);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<GroupDto>(JsonSerializationHelper.Options);

        // Act
        var response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/groups/{created!.Id}");

        // Assert: 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<GroupDetailDto>(JsonSerializationHelper.Options);
        details.Should().NotBeNull();
        details!.Id.Should().Be(created.Id);
        details.Name.Should().Be("مجموعة التفوق");
    }

    [Fact]
    public async Task ScheduleSession_ValidRequest_CreatesSessionAndReturnsCreated()
    {
        // Arrange
        var (workspaceId, academicYearId, gradeId, subjectId, ownerClient, _) = await SetupWorkspaceAsync();

        var createGroupReq = new CreateGroupRequest(academicYearId, gradeId, subjectId, "مجموعة الفيزياء", null, null);
        var groupResp = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/groups", createGroupReq, JsonSerializationHelper.Options);
        groupResp.EnsureSuccessStatusCode();
        var group = await groupResp.Content.ReadFromJsonAsync<GroupDto>(JsonSerializationHelper.Options);

        var sessionStartsAt = DateTimeOffset.UtcNow.AddDays(2);
        var sessionEndsAt = sessionStartsAt.AddHours(2);
        var sessionRequest = new ScheduleSessionRequest(
            StartsAt: sessionStartsAt,
            EndsAt: sessionEndsAt,
            LateGraceMinutes: 15,
            RoomName: "مختبر الفيزياء",
            Topic: "قوانين نيوتن للحركة"
        );

        // Act
        var response = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/groups/{group!.Id}/sessions",
            sessionRequest,
            JsonSerializationHelper.Options);

        // Assert: 201 Created
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var session = await response.Content.ReadFromJsonAsync<SessionDto>(JsonSerializationHelper.Options);
        session.Should().NotBeNull();
        session!.GroupId.Should().Be(group.Id);
        session.Topic.Should().Be("قوانين نيوتن للحركة");
        session.RoomName.Should().Be("مختبر الفيزياء");
        session.Status.Should().Be("Scheduled");

        // Verify in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var dbSession = await db.Sessions.FindAsync(session.Id);
        dbSession.Should().NotBeNull();
        dbSession!.Status.Should().Be(SessionStatus.Scheduled);
    }

    [Fact]
    public async Task GetGroupSessions_ReturnsOkWithList()
    {
        // Arrange
        var (workspaceId, academicYearId, gradeId, subjectId, ownerClient, _) = await SetupWorkspaceAsync();

        var createGroupReq = new CreateGroupRequest(academicYearId, gradeId, subjectId, "مجموعة الكيمياء", null, null);
        var groupResp = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/groups", createGroupReq, JsonSerializationHelper.Options);
        groupResp.EnsureSuccessStatusCode();
        var group = await groupResp.Content.ReadFromJsonAsync<GroupDto>(JsonSerializationHelper.Options);

        var sessionRequest = new ScheduleSessionRequest(
            StartsAt: DateTimeOffset.UtcNow.AddDays(1),
            EndsAt: DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            Topic: "الروابط الكيميائية"
        );
        var sessResp = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/groups/{group!.Id}/sessions", sessionRequest, JsonSerializationHelper.Options);
        sessResp.EnsureSuccessStatusCode();
        var scheduledSession = await sessResp.Content.ReadFromJsonAsync<SessionDto>(JsonSerializationHelper.Options);

        // Act
        var response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/groups/{group.Id}/sessions");

        // Assert: 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedSessions = await response.Content.ReadFromJsonAsync<PagedList<SessionDto>>(JsonSerializationHelper.Options);
        pagedSessions.Should().NotBeNull();
        pagedSessions!.Items.Should().Contain(s => s.Id == scheduledSession!.Id);
    }

    [Fact]
    public async Task CompleteSession_ValidScheduledSession_MarksSessionCompleted()
    {
        // Arrange
        var (workspaceId, academicYearId, gradeId, subjectId, ownerClient, _) = await SetupWorkspaceAsync();

        var createGroupReq = new CreateGroupRequest(academicYearId, gradeId, subjectId, "مجموعة الأحياء", null, null);
        var groupResp = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/groups", createGroupReq, JsonSerializationHelper.Options);
        groupResp.EnsureSuccessStatusCode();
        var group = await groupResp.Content.ReadFromJsonAsync<GroupDto>(JsonSerializationHelper.Options);

        var sessionReq = new ScheduleSessionRequest(
            StartsAt: DateTimeOffset.UtcNow.AddHours(-2),
            EndsAt: DateTimeOffset.UtcNow.AddHours(-1),
            Topic: "الخلية الحية"
        );
        var sessResp = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/groups/{group!.Id}/sessions", sessionReq, JsonSerializationHelper.Options);
        sessResp.EnsureSuccessStatusCode();
        var session = await sessResp.Content.ReadFromJsonAsync<SessionDto>(JsonSerializationHelper.Options);

        // Act
        var response = await ownerClient.PostAsync(
            $"/api/workspaces/{workspaceId}/sessions/{session!.Id}/complete",
            null);

        // Assert: 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await response.Content.ReadFromJsonAsync<SessionCompletedSummaryDto>(JsonSerializationHelper.Options);
        completed.Should().NotBeNull();
        completed!.SessionId.Should().Be(session.Id);

        // Verify in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var dbSession = await db.Sessions.FindAsync(session.Id);
        dbSession.Should().NotBeNull();
        dbSession!.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public async Task CancelSession_ValidScheduledSession_MarksSessionCancelled()
    {
        // Arrange
        var (workspaceId, academicYearId, gradeId, subjectId, ownerClient, _) = await SetupWorkspaceAsync();

        var createGroupReq = new CreateGroupRequest(academicYearId, gradeId, subjectId, "مجموعة التاريخ", null, null);
        var groupResp = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/groups", createGroupReq, JsonSerializationHelper.Options);
        groupResp.EnsureSuccessStatusCode();
        var group = await groupResp.Content.ReadFromJsonAsync<GroupDto>(JsonSerializationHelper.Options);

        var sessionReq = new ScheduleSessionRequest(
            StartsAt: DateTimeOffset.UtcNow.AddDays(3),
            EndsAt: DateTimeOffset.UtcNow.AddDays(3).AddHours(2),
            Topic: "الحضارة المصرية القديمة"
        );
        var sessResp = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/groups/{group!.Id}/sessions", sessionReq, JsonSerializationHelper.Options);
        sessResp.EnsureSuccessStatusCode();
        var session = await sessResp.Content.ReadFromJsonAsync<SessionDto>(JsonSerializationHelper.Options);

        var cancelRequest = new CancelSessionRequest("ظروف طارئة للمدرس");

        // Act
        var response = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/sessions/{session!.Id}/cancel",
            cancelRequest,
            JsonSerializationHelper.Options);

        // Assert: 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelled = await response.Content.ReadFromJsonAsync<SessionCancelledDto>(JsonSerializationHelper.Options);
        cancelled.Should().NotBeNull();
        cancelled!.SessionId.Should().Be(session.Id);
        cancelled.CancellationReason.Should().Be("ظروف طارئة للمدرس");

        // Verify in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var dbSession = await db.Sessions.FindAsync(session.Id);
        dbSession.Should().NotBeNull();
        dbSession!.Status.Should().Be(SessionStatus.Cancelled);
        dbSession.CancellationReason.Should().Be("ظروف طارئة للمدرس");
    }
}
