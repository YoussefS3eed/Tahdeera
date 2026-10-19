using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Endpoints;

[Collection("IntegrationTests")]
public class StudentsControllerTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public StudentsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid WorkspaceId, HttpClient OwnerClient)> ProvisionWorkspaceAsync(int maxStudentsOverride = 0)
    {
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "TEACHER_STARTER");

        if (!await db.AcademicYears.AnyAsync())
        {
            db.AcademicYears.Add(new Tahdeera.Domain.Entities.AcademicYear
            {
                Name = "2026/2027",
                StartsOn = new DateOnly(2026, 9, 1),
                EndsOn = new DateOnly(2027, 6, 30),
                IsCurrent = true
            });
            await db.SaveChangesAsync();
        }

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("stu_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace.CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("Center"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new Tahdeera.Application.Features.Workspaces.DTOs.CreateOwnerDto(
                Username: "usr_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Owner User",
                Phone: TestDataGenerator.GenerateEgyptianPhone()
            ),
            Subscription: new Tahdeera.Application.Features.Workspaces.DTOs.CreateInitialSubscriptionDto(
                PlanId: plan.Id,
                StartsAt: DateTimeOffset.UtcNow,
                EndsAt: DateTimeOffset.UtcNow.AddDays(365),
                AgreedPrice: plan.DefaultPrice,
                InitialPayment: new Tahdeera.Application.Features.Workspaces.DTOs.CreateInitialPaymentDto(plan.DefaultPrice, "Cash", null, null)
            )
        );

        var wsResponse = await adminClient.PostAsJsonAsync("/api/admin/workspaces", createWsCommand, JsonSerializationHelper.Options);
        if (!wsResponse.IsSuccessStatusCode)
        {
            var err = await wsResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create workspace: {wsResponse.StatusCode} - {err}");
        }
        var wsResult = await wsResponse.Content.ReadFromJsonAsync<Tahdeera.Application.Features.Workspaces.DTOs.CreateWorkspaceResultDto>(JsonSerializationHelper.Options);
        var workspaceId = wsResult!.WorkspaceId;

        if (maxStudentsOverride > 0)
        {
            using var updateScope = _factory.Services.CreateScope();
            var updateDb = updateScope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
            var sub = await updateDb.WorkspaceSubscriptions.FirstAsync(s => s.WorkspaceId == workspaceId);
            sub.MaxStudentsOverride = maxStudentsOverride;
            await updateDb.SaveChangesAsync();
        }

        var ownerClient = await _factory.CreateAuthenticatedClientAsync(ownerEmail, ownerPassword);
        ownerClient.DefaultRequestHeaders.Add("X-Workspace-Id", workspaceId.ToString());

        return (workspaceId, ownerClient);
    }

    [Fact]
    public async Task RegisterStudent_ValidRequest_ShouldCreateStudentAndReturn201()
    {
        // Arrange
        var (workspaceId, ownerClient) = await ProvisionWorkspaceAsync();

        var request = new RegisterStudentRequest(
            FullName: "محمد أحمد محمود",
            GradeId: 10,
            GuardianName: "أحمد محمود",
            GuardianPhone: "01096817311",
            StudentPhone: "01012345678",
            WhatsappOptIn: true,
            SeatNumber: "A-01",
            LocalNotes: "طالب متفوق",
            CardToken: "nfc_raw_token_xyz123",
            CardUidDisplay: "Card #101"
        );

        // Act
        var response = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/students", request, JsonSerializationHelper.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var student = await response.Content.ReadFromJsonAsync<StudentEnrollmentDto>(JsonSerializationHelper.Options);
        student.Should().NotBeNull();
        student!.StudentCode.Should().MatchRegex(@"^STU-\d{4}-\d{5}$");
        student.FullName.Should().Be("محمد أحمد محمود");
        student.GradeId.Should().Be(10);
        student.GradeNameAr.Should().Be("الصف الأول الثانوي");
        student.AssignedCredentialId.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterStudent_DuplicateGuardianPhone_ShouldRequireSiblingBypass()
    {
        // Arrange
        var (workspaceId, ownerClient) = await ProvisionWorkspaceAsync();
        var guardianPhone = "01012345678";

        var request1 = new RegisterStudentRequest(
            FullName: "الطالب الأول",
            GradeId: 10,
            GuardianName: "ولي الأمر",
            GuardianPhone: guardianPhone,
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            CardToken: null,
            CardUidDisplay: null
        );

        var res1 = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/students", request1, JsonSerializationHelper.Options);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act 1: Second student with same guardian phone, AllowSiblingPhone = false
        var request2 = new RegisterStudentRequest(
            FullName: "الطالب الثاني (أخ)",
            GradeId: 11,
            GuardianName: "ولي الأمر",
            GuardianPhone: guardianPhone,
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            CardToken: null,
            CardUidDisplay: null,
            AllowSiblingPhone: false
        );

        var res2 = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/students", request2, JsonSerializationHelper.Options);

        // Assert 1: Conflict 409 GUARDIAN_PHONE_ALREADY_EXISTS_CONFIRM_SIBLING
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Act 2: Retry with AllowSiblingPhone = true
        var request3 = request2 with { AllowSiblingPhone = true };
        var res3 = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/students", request3, JsonSerializationHelper.Options);

        // Assert 2: Created 201
        res3.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task LinkStudentByCard_ToSecondWorkspace_ShouldInheritGradeAndPreventDataLeak()
    {
        // Arrange: Workspace A enrolls student with Card
        var (workspaceA, clientA) = await ProvisionWorkspaceAsync();
        var (workspaceB, clientB) = await ProvisionWorkspaceAsync();

        var cardToken = "shared_universal_card_" + Guid.NewGuid().ToString("N");
        var regRequest = new RegisterStudentRequest(
            FullName: "يوسف سمير إبراهيم",
            GradeId: 11,
            GuardianName: "سمير إبراهيم",
            GuardianPhone: "01099887766",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: "Seat-A",
            LocalNotes: "ملاحظات مركز أ السرية",
            CardToken: cardToken,
            CardUidDisplay: "Card #99"
        );

        var regRes = await clientA.PostAsJsonAsync($"/api/workspaces/{workspaceA}/students", regRequest, JsonSerializationHelper.Options);
        regRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var regDto = await regRes.Content.ReadFromJsonAsync<StudentEnrollmentDto>(JsonSerializationHelper.Options);

        // Act: Workspace B links student by scanning Card
        var linkRequest = new LinkStudentByCardRequest(
            ScannedToken: cardToken,
            GradeId: null, // Inherits Grade 11
            GuardianName: "سمير إبراهيم - فرع ب",
            GuardianPhone: "01099887766",
            StudentPhone: null,
            WhatsappOptIn: true,
            SeatNumber: "Seat-B",
            LocalNotes: "ملاحظات مركز ب"
        );

        var linkRes = await clientB.PostAsJsonAsync($"/api/workspaces/{workspaceB}/students/link-by-card", linkRequest, JsonSerializationHelper.Options);

        // Assert
        linkRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var linkedStudent = await linkRes.Content.ReadFromJsonAsync<StudentEnrollmentDto>(JsonSerializationHelper.Options);
        linkedStudent!.StudentId.Should().Be(regDto!.StudentId);
        linkedStudent.GradeId.Should().Be(11);
        linkedStudent.GradeNameAr.Should().Be("الصف الثاني الثانوي");

        // Verify Workspace B's 360 profile does NOT contain Workspace A's notes
        var profileRes = await clientB.GetAsync($"/api/workspaces/{workspaceB}/students/{regDto.StudentId}/profile");
        profileRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await profileRes.Content.ReadFromJsonAsync<Student360ProfileDto>(JsonSerializationHelper.Options);
        profile!.WorkspaceEnrollment.LocalNotes.Should().Be("ملاحظات مركز ب");
        profile.WorkspaceEnrollment.LocalNotes.Should().NotContain("مركز أ");
    }

    [Fact]
    public async Task LinkStudentByCode_WithMatchingGuardianPhone_ShouldSucceed()
    {
        // Arrange: Workspace A enrolls student
        var (workspaceA, clientA) = await ProvisionWorkspaceAsync();
        var (workspaceB, clientB) = await ProvisionWorkspaceAsync();

        var guardianPhone = "01055443322";
        var regRequest = new RegisterStudentRequest(
            FullName: "كريم حسام الدين",
            GradeId: 12,
            GuardianName: "حسام الدين",
            GuardianPhone: guardianPhone,
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            CardToken: null,
            CardUidDisplay: null
        );

        var regRes = await clientA.PostAsJsonAsync($"/api/workspaces/{workspaceA}/students", regRequest, JsonSerializationHelper.Options);
        var regDto = await regRes.Content.ReadFromJsonAsync<StudentEnrollmentDto>(JsonSerializationHelper.Options);

        // Act 1: Invalid phone factor -> fails with 400 STUDENT_ENROLLMENT_VERIFICATION_FAILED
        var badLink = new LinkStudentByCodeRequest(
            StudentCode: regDto!.StudentCode,
            RegisteredGuardianPhone: "01099990000", // Wrong phone
            GradeId: null,
            GuardianName: null,
            GuardianPhone: null,
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null
        );

        var badRes = await clientB.PostAsJsonAsync($"/api/workspaces/{workspaceB}/students/link-by-code", badLink, JsonSerializationHelper.Options);
        badRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Act 2: Correct phone factor -> succeeds
        var goodLink = badLink with { RegisteredGuardianPhone = guardianPhone, GuardianPhone = guardianPhone };
        var goodRes = await clientB.PostAsJsonAsync($"/api/workspaces/{workspaceB}/students/link-by-code", goodLink, JsonSerializationHelper.Options);
        goodRes.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task QuotaRejection_ShouldReturn422WhenCapacityReached()
    {
        // Arrange: Workspace with max 1 student
        var (workspaceId, ownerClient) = await ProvisionWorkspaceAsync(maxStudentsOverride: 1);

        var reg1 = new RegisterStudentRequest("طالب 1", 10, "ولي أمر 1", "01011110001", null, false, null, null, null, null);
        var res1 = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/students", reg1, JsonSerializationHelper.Options);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act: Enrolling student 2 exceeds quota
        var reg2 = new RegisterStudentRequest("طالب 2", 10, "ولي أمر 2", "01011110002", null, false, null, null, null, null);
        var res2 = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/students", reg2, JsonSerializationHelper.Options);

        // Assert: 422 Unprocessable QUOTA_MAX_STUDENTS_REACHED
        res2.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task LinkStudentByCard_DuplicateGuardianPhoneWithoutSiblingConfirmation_ShouldReturnConflict()
    {
        // Arrange
        var (workspaceA, clientA) = await ProvisionWorkspaceAsync();
        var (workspaceB, clientB) = await ProvisionWorkspaceAsync();

        var sharedGuardianPhone = "01099887711";
        var cardToken = "card_token_sibling_test_" + Guid.NewGuid().ToString("N");

        // Enroll Student 1 in Workspace A with cardToken
        var regA = new RegisterStudentRequest(
            FullName: "طالب أ في مركز 1",
            GradeId: 10,
            GuardianName: "ولي أمر",
            GuardianPhone: "01011112233",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            CardToken: cardToken,
            CardUidDisplay: "Card #S1"
        );
        var regResA = await clientA.PostAsJsonAsync($"/api/workspaces/{workspaceA}/students", regA, JsonSerializationHelper.Options);
        regResA.StatusCode.Should().Be(HttpStatusCode.Created);

        // Enroll Student 2 in Workspace B with sharedGuardianPhone
        var regB = new RegisterStudentRequest(
            FullName: "طالب شقيق مقيد في مركز 2",
            GradeId: 11,
            GuardianName: "ولي أمر الشقيق",
            GuardianPhone: sharedGuardianPhone,
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            CardToken: null,
            CardUidDisplay: null
        );
        var regResB = await clientB.PostAsJsonAsync($"/api/workspaces/{workspaceB}/students", regB, JsonSerializationHelper.Options);
        regResB.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act 1: Workspace B tries to link Student 1 using sharedGuardianPhone with AllowSiblingPhone = false
        var linkRequest = new LinkStudentByCardRequest(
            ScannedToken: cardToken,
            GradeId: null,
            GuardianName: "ولي أمر الشقيق",
            GuardianPhone: sharedGuardianPhone,
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            AllowSiblingPhone: false
        );
        var linkRes1 = await clientB.PostAsJsonAsync($"/api/workspaces/{workspaceB}/students/link-by-card", linkRequest, JsonSerializationHelper.Options);

        // Assert 1: Conflict 409 GUARDIAN_PHONE_ALREADY_EXISTS_CONFIRM_SIBLING
        linkRes1.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Act 2: Workspace B retries with AllowSiblingPhone = true
        var linkRequestAllowed = linkRequest with { AllowSiblingPhone = true };
        var linkRes2 = await clientB.PostAsJsonAsync($"/api/workspaces/{workspaceB}/students/link-by-card", linkRequestAllowed, JsonSerializationHelper.Options);

        // Assert 2: Created 201
        linkRes2.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CrossWorkspaceAccess_WhenUserNotMember_ShouldReturnForbidden()
    {
        // Arrange
        var (workspaceA, _) = await ProvisionWorkspaceAsync();
        var (_, clientB) = await ProvisionWorkspaceAsync();

        // Act: User belonging to Workspace B tries to access Workspace A's student roster
        var response = await clientB.GetAsync($"/api/workspaces/{workspaceA}/students");

        // Assert: 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
