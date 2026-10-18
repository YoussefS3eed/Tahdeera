using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.Application.Features.Auth.DTOs;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class WorkspaceMembersTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public WorkspaceMembersTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid WorkspaceId, string OwnerEmail, string OwnerPassword, HttpClient OwnerClient)> ProvisionWorkspaceAsync(string planCode = "TEACHER_STARTER")
    {
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == planCode);

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("owner");
        var ownerPassword = "OwnerPassword123!";
        var command = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("Center"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Center Owner",
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

        var response = await adminClient.PostAsJsonAsync("/api/admin/workspaces", command, JsonSerializationHelper.Options);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateWorkspaceResultDto>(JsonSerializationHelper.Options);

        var ownerClient = await _factory.CreateAuthenticatedClientAsync(ownerEmail, ownerPassword);
        ownerClient.DefaultRequestHeaders.Add("X-Workspace-Id", result!.WorkspaceId.ToString());

        return (result.WorkspaceId, ownerEmail, ownerPassword, ownerClient);
    }

    [Fact]
    public async Task GetWorkspaceMembers_ReturnsActiveMembers()
    {
        // Arrange
        var (workspaceId, _, _, ownerClient) = await ProvisionWorkspaceAsync();

        // Act: Center owner queries active team members
        var response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/members");

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var members = await response.Content.ReadFromJsonAsync<List<WorkspaceMemberDto>>(JsonSerializationHelper.Options);
        members.Should().NotBeNull();
        members.Should().ContainSingle(m => m.Role == "Owner" && m.IsActive);
    }

    [Fact]
    public async Task AddWorkspaceMember_WithNewUser_CreatesIdentityAndMember()
    {
        // Arrange: Provision workspace on professional plan with higher quota
        var (workspaceId, _, _, ownerClient) = await ProvisionWorkspaceAsync("PROFESSIONAL_CENTER");
        var staffEmail = TestDataGenerator.GenerateUniqueEmail("teacher");
        var staffPhone = TestDataGenerator.GenerateEgyptianPhone();
        var staffUsername = "tchr_" + Guid.NewGuid().ToString("N")[..6];

        var request = new AddWorkspaceMemberRequest(
            Username: staffUsername,
            Email: staffEmail,
            Password: "TeacherPass123!",
            DisplayName: "Math Teacher",
            Phone: staffPhone,
            Role: "Teacher"
        );

        // Act: Owner adds new staff member
        var response = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/members", request, JsonSerializationHelper.Options);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AddWorkspaceMemberResultDto>(JsonSerializationHelper.Options);
        result.Should().NotBeNull();
        result!.MemberId.Should().NotBeEmpty();
        result.UserId.Should().NotBeEmpty();
        result.Role.Should().Be("Teacher");
        result.IsActive.Should().BeTrue();

        // Assert Database records in PostgreSQL
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var userInDb = await db.AppUsers.FindAsync(result.UserId);
        userInDb.Should().NotBeNull();
        userInDb!.NormalizedEmail.Should().Be(staffEmail.ToUpperInvariant());

        var memberInDb = await db.WorkspaceMembers.FindAsync(result.MemberId);
        memberInDb.Should().NotBeNull();
        memberInDb!.Role.Should().Be("Teacher");
        memberInDb.IsActive.Should().BeTrue();

        var auditInDb = await db.AuditLogs.FirstOrDefaultAsync(a => a.WorkspaceId == workspaceId && a.Action == "MEMBER_ADDED");
        auditInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task AddWorkspaceMember_WithExistingUser_LinksUserWithoutPasswordReset()
    {
        // Arrange: Create an existing global user in PostgreSQL
        var existingEmail = TestDataGenerator.GenerateUniqueEmail("existing");
        var existingPhone = TestDataGenerator.GenerateEgyptianPhone();
        Guid existingUserId;

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
            var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

            var identityUser = new IdentityUser<Guid>
            {
                Id = Guid.NewGuid(),
                UserName = "exist_" + Guid.NewGuid().ToString("N")[..6],
                Email = existingEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(identityUser, "ExistingPass123!");

            var appUser = new AppUser
            {
                IdentitySubject = identityUser.Id.ToString(),
                Username = identityUser.UserName,
                DisplayName = "Existing Global User",
                NormalizedEmail = existingEmail.ToUpperInvariant(),
                Phone = existingPhone,
                PreferredLanguage = "ar",
                PreferredTheme = "light",
                IsPlatformAdmin = false,
                IsActive = true
            };
            await db.AppUsers.AddAsync(appUser);
            await db.SaveChangesAsync();
            existingUserId = appUser.Id;
        }

        // Provision workspace
        var (workspaceId, _, _, ownerClient) = await ProvisionWorkspaceAsync("PROFESSIONAL_CENTER");

        var request = new AddWorkspaceMemberRequest(
            Username: "any_username",
            Email: existingEmail,
            Password: "IrrelevantPassword123!",
            DisplayName: "Assistant",
            Phone: existingPhone,
            Role: "Assistant"
        );

        // Act: Owner invites existing user
        var response = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/members", request, JsonSerializationHelper.Options);

        // Assert HTTP response: 201 Created
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AddWorkspaceMemberResultDto>(JsonSerializationHelper.Options);
        result.Should().NotBeNull();
        result!.UserId.Should().Be(existingUserId, "Must link to the existing AppUser record without creating a new user.");

        // Assert Database records: Password and Identity unchanged
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
            var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

            var memberInDb = await db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == existingUserId);
            memberInDb.Should().NotBeNull();
            memberInDb!.Role.Should().Be("Assistant");

            // Verify original password still works
            var identityUser = await userManager.FindByEmailAsync(existingEmail);
            var isPasswordSame = await userManager.CheckPasswordAsync(identityUser!, "ExistingPass123!");
            isPasswordSame.Should().BeTrue("Existing user password MUST NOT be reset or modified.");
        }
    }

    [Fact]
    public async Task AddWorkspaceMember_ExceedingQuota_Returns422QuotaMaxStaffReached()
    {
        // Arrange: Workspace on TEACHER_STARTER (maxStaff = 1)
        var (workspaceId, _, _, ownerClient) = await ProvisionWorkspaceAsync("TEACHER_STARTER");

        // Add 1st Staff Member (Quota = 1, should succeed)
        var staff1 = new AddWorkspaceMemberRequest(
            Username: "staff1_" + Guid.NewGuid().ToString("N")[..6],
            Email: TestDataGenerator.GenerateUniqueEmail("staff1"),
            Password: "Password123!",
            DisplayName: "Staff One",
            Phone: TestDataGenerator.GenerateEgyptianPhone(),
            Role: "Assistant"
        );
        var add1Response = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/members", staff1, JsonSerializationHelper.Options);
        add1Response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act: Attempt to add 2nd Staff Member (Quota overflow!)
        var staff2 = new AddWorkspaceMemberRequest(
            Username: "staff2_" + Guid.NewGuid().ToString("N")[..6],
            Email: TestDataGenerator.GenerateUniqueEmail("staff2"),
            Password: "Password123!",
            DisplayName: "Staff Two",
            Phone: TestDataGenerator.GenerateEgyptianPhone(),
            Role: "Teacher"
        );
        var add2Response = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/members", staff2, JsonSerializationHelper.Options);

        // Assert RFC 9457 ProblemDetails with 422 Unprocessable Entity
        var problem = await add2Response.ShouldHaveProblemDetails(HttpStatusCode.UnprocessableEntity, "QUOTA_MAX_STAFF_REACHED");
        problem.Detail.Should().NotBeNullOrWhiteSpace();

        // Assert Database state: Exactly 1 non-owner active member exists
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var nonOwnerCount = await db.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspaceId && m.Role != "Owner" && m.IsActive);
        nonOwnerCount.Should().Be(1);
    }

    [Fact]
    public async Task DeactivateWorkspaceMember_Success_RevokesRefreshTokensImmediately()
    {
        // Arrange: Provision workspace, add an assistant
        var (workspaceId, _, _, ownerClient) = await ProvisionWorkspaceAsync("PROFESSIONAL_CENTER");
        var assistantEmail = TestDataGenerator.GenerateUniqueEmail("assistant");
        var assistantPassword = "AssistantPass123!";
        var addRequest = new AddWorkspaceMemberRequest(
            Username: "asst_" + Guid.NewGuid().ToString("N")[..6],
            Email: assistantEmail,
            Password: assistantPassword,
            DisplayName: "Assistant to Deactivate",
            Phone: TestDataGenerator.GenerateEgyptianPhone(),
            Role: "Assistant"
        );
        var addResponse = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/members", addRequest, JsonSerializationHelper.Options);
        var addResult = await addResponse.Content.ReadFromJsonAsync<AddWorkspaceMemberResultDto>(JsonSerializationHelper.Options);

        // Assistant logs in to acquire active refresh token
        var anonClient = _factory.CreateClient();
        var loginResponse = await anonClient.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmailOrPhone = assistantEmail,
            password = assistantPassword
        }, JsonSerializationHelper.Options);
        loginResponse.EnsureSuccessStatusCode();
        var assistantAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResultDto>(JsonSerializationHelper.Options);

        // Act: Owner deactivates assistant
        var deactivateResponse = await ownerClient.PostAsync($"/api/workspaces/{workspaceId}/members/{addResult!.MemberId}/deactivate", null);

        // Assert HTTP response
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deactivateResult = await deactivateResponse.Content.ReadFromJsonAsync<DeactivateWorkspaceMemberResultDto>(JsonSerializationHelper.Options);
        deactivateResult.Should().NotBeNull();
        deactivateResult!.IsActive.Should().BeFalse();
        deactivateResult.LeftAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));

        // Assert Immediate Session Invalidation: Assistant attempts to refresh token
        var refreshResponse = await anonClient.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = assistantAuth!.RefreshToken
        }, JsonSerializationHelper.Options);

        // Must be rejected with 401 Unauthorized
        await refreshResponse.ShouldHaveProblemDetails(HttpStatusCode.Unauthorized, "AUTH_REFRESH_TOKEN_REVOKED");

        // Assert Database state in PostgreSQL
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var memberInDb = await db.WorkspaceMembers.FindAsync(addResult.MemberId);
        memberInDb.Should().NotBeNull();
        memberInDb!.IsActive.Should().BeFalse();
        memberInDb.LeftAt.Should().HaveValue();

        var activeTokens = await db.RefreshTokens
            .Where(r => r.UserId == addResult.UserId && r.RevokedAt == null)
            .ToListAsync();
        activeTokens.Should().BeEmpty("Deactivation must immediately revoke all active refresh tokens for the user.");

        var auditInDb = await db.AuditLogs.FirstOrDefaultAsync(a => a.WorkspaceId == workspaceId && a.Action == "MEMBER_DEACTIVATED");
        auditInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task DeactivateWorkspaceMember_WhenOwner_Returns400CannotDeactivateOwner()
    {
        // Arrange
        var (workspaceId, _, _, ownerClient) = await ProvisionWorkspaceAsync();

        // Query owner member ID
        var membersResponse = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/members");
        var members = await membersResponse.Content.ReadFromJsonAsync<List<WorkspaceMemberDto>>(JsonSerializationHelper.Options);
        var ownerMember = members!.First(m => m.Role == "Owner");

        // Act: Attempt to deactivate the workspace owner
        var response = await ownerClient.PostAsync($"/api/workspaces/{workspaceId}/members/{ownerMember.Id}/deactivate", null);

        // Assert RFC 9457 ProblemDetails with 400 Bad Request
        var problem = await response.ShouldHaveProblemDetails(HttpStatusCode.BadRequest, "CANNOT_DEACTIVATE_OWNER");
        problem.Detail.Should().NotBeNullOrWhiteSpace();

        // Assert Database state: Owner remains active
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var ownerInDb = await db.WorkspaceMembers.FindAsync(ownerMember.Id);
        ownerInDb.Should().NotBeNull();
        ownerInDb!.IsActive.Should().BeTrue();
        ownerInDb.LeftAt.Should().BeNull();
    }
}
