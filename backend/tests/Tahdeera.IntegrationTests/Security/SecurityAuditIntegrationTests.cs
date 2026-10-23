using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Security;

[Collection("IntegrationTests")]
public class SecurityAuditIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public SecurityAuditIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid WorkspaceId, Guid AcademicYearId, short GradeId, Guid SubjectId, HttpClient OwnerClient, string OwnerEmail)> SetupWorkspaceAsync(string namePrefix)
    {
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "TEACHER_STARTER");

        var academicYear = await db.AcademicYears.FirstOrDefaultAsync(ay => ay.IsCurrent);
        if (academicYear == null)
        {
            academicYear = new AcademicYear
            {
                Id = Guid.NewGuid(),
                Name = "2026/2027",
                StartsOn = new DateOnly(2026, 9, 1),
                EndsOn = new DateOnly(2027, 6, 30),
                IsCurrent = true
            };
            db.AcademicYears.Add(academicYear);
            await db.SaveChangesAsync();
        }

        var grade = await db.Grades.OrderBy(g => g.SortOrder).FirstAsync();

        var subject = await db.Subjects.FirstOrDefaultAsync();
        if (subject == null)
        {
            subject = new Subject
            {
                Id = Guid.NewGuid(),
                Code = "SUBJ_" + Guid.NewGuid().ToString("N")[..6],
                NameAr = "مادة تجريبية",
                NameEn = "Test Subject",
                IsActive = true
            };
            db.Subjects.Add(subject);
            await db.SaveChangesAsync();
        }

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail(namePrefix + "_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName(namePrefix),
            Kind: "Teacher",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo, Egypt",
            DefaultLateGraceMinutes: 15,
            Owner: new CreateOwnerDto(
                Username: namePrefix + "_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: namePrefix + " Owner",
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
    public async Task DefensiveSecurityHeaders_OnAnyRequest_ArePresentAndHardened()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");

        response.Headers.Contains("X-Frame-Options").Should().BeTrue();
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");

        response.Headers.Contains("Referrer-Policy").Should().BeTrue();
        response.Headers.GetValues("Referrer-Policy").First().Should().Be("strict-origin-when-cross-origin");

        response.Headers.Contains("Content-Security-Policy").Should().BeTrue();
        response.Headers.GetValues("Content-Security-Policy").First().Should().Contain("frame-ancestors 'none'");

        response.Headers.Contains("Permissions-Policy").Should().BeTrue();
    }

    [Fact]
    public async Task AnonymousRequest_ToProtectedWorkspaceEndpoint_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/workspaces/{Guid.NewGuid()}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MalformedOrForgedToken_ToProtectedEndpoint_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.e30.tampered_signature");

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthRateLimiting_WhenPermitsExceeded_Returns429TooManyRequestsWithProblemDetails()
    {
        // Arrange: Dedicated IP partition to avoid impacting other test executions
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.199");

        var payload = new
        {
            usernameOrEmailOrPhone = "nonexistent_bruteforce_user@test.com",
            password = "WrongPassword123!"
        };

        HttpResponseMessage? lastResponse = null;

        // Act: Fire 15 consecutive login attempts (rate limit permit limit is 10)
        for (int i = 0; i < 15; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/auth/login", payload, JsonSerializationHelper.Options);
            if (lastResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                break;
            }
        }

        // Assert
        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        lastResponse.Headers.Contains("Retry-After").Should().BeTrue();

        var problem = await lastResponse.ShouldHaveProblemDetails(HttpStatusCode.TooManyRequests, "RATE_LIMIT_EXCEEDED");
        problem.Detail.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CrossWorkspace_AttendanceHistory_WhenStudentBelongsToAnotherWorkspace_IsRejected()
    {
        // Arrange
        var (wsA, ayIdA, gradeIdA, subjIdA, clientA, _) = await SetupWorkspaceAsync("SecWsA");
        var (wsB, _, _, _, _, _) = await SetupWorkspaceAsync("SecWsB");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        // Student enrolled in Workspace B only
        var student = new Student { Id = Guid.NewGuid(), FullName = "Isolated Student B", StudentCode = "STU-SEC-001" };
        var wsStudentB = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsB,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active
        };
        db.Students.Add(student);
        db.WorkspaceStudents.Add(wsStudentB);
        await db.SaveChangesAsync();

        // Act: Workspace A calls attendance history for Student B
        var response = await clientA.GetAsync($"/api/workspaces/{wsA}/students/{student.Id}/attendance");

        // Assert: Access rejected with 404 NotFound and STUDENT_NOT_ENROLLED_IN_GROUP
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.ShouldHaveProblemDetails(HttpStatusCode.NotFound, "STUDENT_NOT_ENROLLED_IN_GROUP");
        problem.Detail.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CrossWorkspace_AttendanceHistory_WhenStudentEnrolledInBothWorkspaces_StrictlyIsolatesRecords()
    {
        // Arrange
        var (wsA, ayIdA, gradeIdA, subjIdA, clientA, _) = await SetupWorkspaceAsync("IsoWsA");
        var (wsB, ayIdB, gradeIdB, subjIdB, clientB, _) = await SetupWorkspaceAsync("IsoWsB");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var adminUser = await db.AppUsers.FirstAsync(u => u.IsPlatformAdmin);

        // Common student enrolled in both workspaces
        var student = new Student { Id = Guid.NewGuid(), FullName = "Shared Student", StudentCode = "STU-SEC-002" };
        var wsStudentA = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = wsA, StudentId = student.Id, Status = WorkspaceStudentStatus.Active };
        var wsStudentB = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = wsB, StudentId = student.Id, Status = WorkspaceStudentStatus.Active };
        db.Students.Add(student);
        db.WorkspaceStudents.AddRange(wsStudentA, wsStudentB);

        // Group & Session in Workspace A
        var groupA = Group.Create(wsA, ayIdA, gradeIdA, "Public Group In Workspace A", adminUser.Id);
        db.Groups.Add(groupA);
        var sessionA = Session.Create(groupA.Id, DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow, 30, adminUser.Id);
        db.Sessions.Add(sessionA);
        var recA = AttendanceRecord.CreateCheckIn(sessionA, student.Id, wsA, DateTimeOffset.UtcNow.AddHours(-2), adminUser.Id);
        db.AttendanceRecords.Add(recA);

        // Group & Session in Workspace B (Sensitive data that must never leak)
        var groupB = Group.Create(wsB, ayIdB, gradeIdB, "TOP_SECRET_WORKSPACE_B_GROUP", adminUser.Id);
        db.Groups.Add(groupB);
        var sessionB = Session.Create(groupB.Id, DateTimeOffset.UtcNow.AddHours(-4), DateTimeOffset.UtcNow.AddHours(-2), 30, adminUser.Id);
        db.Sessions.Add(sessionB);
        var recB = AttendanceRecord.CreateCheckIn(sessionB, student.Id, wsB, DateTimeOffset.UtcNow.AddHours(-4), adminUser.Id);
        db.AttendanceRecords.Add(recB);

        await db.SaveChangesAsync();

        // Act: Workspace A queries student attendance history
        var response = await clientA.GetAsync($"/api/workspaces/{wsA}/students/{student.Id}/attendance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<StudentAttendanceHistoryDto>(JsonSerializationHelper.Options);
        result.Should().NotBeNull();
        result!.TotalSessions.Should().Be(1, "Must only count sessions belonging to the requesting workspace.");
        result.Timeline.Should().HaveCount(1);
        result.Timeline[0].SessionId.Should().Be(sessionA.Id);
        result.Timeline[0].GroupName.Should().Be("Public Group In Workspace A");

        // Ensure zero information leakage of Workspace B's session or group name
        result.Timeline.Should().NotContain(t => t.GroupName.Contains("TOP_SECRET_WORKSPACE_B_GROUP"));
        result.Timeline.Should().NotContain(t => t.SessionId == sessionB.Id);
    }

    [Fact]
    public async Task CrossWorkspace_SessionAttendanceSheet_WithoutHeader_DeniesCrossWorkspaceAccess()
    {
        // Arrange
        var (wsA, _, _, _, clientA, _) = await SetupWorkspaceAsync("SheetWsA");
        var (wsB, ayIdB, gradeIdB, _, _, _) = await SetupWorkspaceAsync("SheetWsB");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var adminUser = await db.AppUsers.FirstAsync(u => u.IsPlatformAdmin);

        var groupB = Group.Create(wsB, ayIdB, gradeIdB, "Group In WS B", adminUser.Id);
        db.Groups.Add(groupB);
        var sessionB = Session.Create(groupB.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2), 25, adminUser.Id);
        db.Sessions.Add(sessionB);
        await db.SaveChangesAsync();

        // Remove X-Workspace-Id header from clientA to test header-less tenant resolution
        clientA.DefaultRequestHeaders.Remove("X-Workspace-Id");

        // Act: User from Workspace A attempts to access Workspace B's session sheet
        var response = await clientA.GetAsync($"/api/workspaces/{wsB}/sessions/{sessionB.Id}/attendance");

        // Assert: Denied (403 Forbidden because User A is not authorized for Workspace B)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CrossWorkspace_AttendanceCorrection_WhenSessionBelongsToAnotherWorkspace_IsRejected()
    {
        // Arrange
        var (wsA, _, _, _, clientA, _) = await SetupWorkspaceAsync("CorrWsA");
        var (wsB, ayIdB, gradeIdB, _, _, _) = await SetupWorkspaceAsync("CorrWsB");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var adminUser = await db.AppUsers.FirstAsync(u => u.IsPlatformAdmin);

        var student = new Student { Id = Guid.NewGuid(), FullName = "Victim Student", StudentCode = "STU-SEC-003" };
        var wsStudentB = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = wsB, StudentId = student.Id, Status = WorkspaceStudentStatus.Active };
        db.Students.Add(student);
        db.WorkspaceStudents.Add(wsStudentB);

        var groupB = Group.Create(wsB, ayIdB, gradeIdB, "Group In WS B", adminUser.Id);
        db.Groups.Add(groupB);
        var sessionB = Session.Create(groupB.Id, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(-1).AddHours(2), 25, adminUser.Id);
        db.Sessions.Add(sessionB);
        var recordB = AttendanceRecord.CreateCheckIn(sessionB, student.Id, wsB, DateTimeOffset.UtcNow.AddDays(-1), adminUser.Id);
        db.AttendanceRecords.Add(recordB);
        await db.SaveChangesAsync();

        // Act: Owner of Workspace A attempts to correct attendance on Workspace B's session using Workspace A's route
        var patchPayload = new CorrectAttendanceRecordRequest(
            AttendanceStatus.Excused,
            "Unauthorized cross-workspace override attempt"
        );

        var response = await clientA.PatchAsJsonAsync(
            $"/api/workspaces/{wsA}/sessions/{sessionB.Id}/attendance/{student.Id}",
            patchPayload,
            JsonSerializationHelper.Options);

        // Assert: Must be rejected with 403 Forbidden and UNAUTHORIZED_WORKSPACE_ACCESS
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.ShouldHaveProblemDetails(HttpStatusCode.Forbidden, "UNAUTHORIZED_WORKSPACE_ACCESS");
        problem.Detail.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CrossWorkspace_ManualCheckIn_WhenSessionBelongsToAnotherWorkspace_IsRejected()
    {
        // Arrange
        var (wsA, _, _, _, clientA, _) = await SetupWorkspaceAsync("ManualWsA");
        var (wsB, ayIdB, gradeIdB, _, _, _) = await SetupWorkspaceAsync("ManualWsB");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var adminUser = await db.AppUsers.FirstAsync(u => u.IsPlatformAdmin);

        var student = new Student { Id = Guid.NewGuid(), FullName = "Target Student", StudentCode = "STU-SEC-004" };
        db.Students.Add(student);

        var groupB = Group.Create(wsB, ayIdB, gradeIdB, "Group B", adminUser.Id);
        db.Groups.Add(groupB);
        var sessionB = Session.Create(groupB.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2), 25, adminUser.Id);
        db.Sessions.Add(sessionB);
        await db.SaveChangesAsync();

        // Act: User in Workspace A attempts to check in student for session in Workspace B via Workspace A's route
        var checkInPayload = new
        {
            studentId = student.Id,
            retroactiveReason = "Cross-workspace exploit"
        };

        var response = await clientA.PostAsJsonAsync(
            $"/api/workspaces/{wsA}/sessions/{sessionB.Id}/attendance/manual",
            checkInPayload,
            JsonSerializationHelper.Options);

        // Assert: Rejected with 403 Forbidden and UNAUTHORIZED_WORKSPACE_ACCESS
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.ShouldHaveProblemDetails(HttpStatusCode.Forbidden, "UNAUTHORIZED_WORKSPACE_ACCESS");
        problem.Detail.Should().NotBeNullOrWhiteSpace();
    }
}
