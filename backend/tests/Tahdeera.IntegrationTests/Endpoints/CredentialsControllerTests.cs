using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.Application.Features.Credentials.DTOs;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Endpoints;

[Collection("IntegrationTests")]
public class CredentialsControllerTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public CredentialsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid WorkspaceId, HttpClient OwnerClient, Guid StudentId)> SetupWorkspaceWithStudentAsync()
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

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("cred_owner");
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

        var ownerClient = await _factory.CreateAuthenticatedClientAsync(ownerEmail, ownerPassword);
        ownerClient.DefaultRequestHeaders.Add("X-Workspace-Id", workspaceId.ToString());

        // Register student
        var regRequest = new RegisterStudentRequest(
            FullName: "طالب تجريبي للاعتماد",
            GradeId: 10,
            GuardianName: "ولي أمر",
            GuardianPhone: TestDataGenerator.GenerateEgyptianPhone(),
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: "C1",
            LocalNotes: null,
            CardToken: null,
            CardUidDisplay: null
        );

        var regResponse = await ownerClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/students", regRequest, JsonSerializationHelper.Options);
        if (!regResponse.IsSuccessStatusCode)
        {
            var err = await regResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to register student: {regResponse.StatusCode} - {err}");
        }
        var regResult = await regResponse.Content.ReadFromJsonAsync<StudentEnrollmentDto>(JsonSerializationHelper.Options);

        return (workspaceId, ownerClient, regResult!.StudentId);
    }

    [Fact]
    public async Task IssueNfc_ShouldIssueCard_AndAutoRevokePriorNfcCard()
    {
        // Arrange
        var (workspaceId, ownerClient, studentId) = await SetupWorkspaceWithStudentAsync();

        var card1Token = "nfc_token_1_" + Guid.NewGuid().ToString("N");
        var card2Token = "nfc_token_2_" + Guid.NewGuid().ToString("N");

        // Act 1: Issue Card 1
        var issue1Response = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/credentials/students/{studentId}/nfc",
            new IssueNfcCredentialRequest(card1Token, "Card #001"),
            JsonSerializationHelper.Options);

        issue1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var card1 = await issue1Response.Content.ReadFromJsonAsync<CredentialIssuedDto>(JsonSerializationHelper.Options);
        card1.Should().NotBeNull();
        card1!.ReplacedCredentialId.Should().BeNull();

        // Act 2: Issue Card 2 (replaces Card 1 per DEC-04)
        var issue2Response = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/credentials/students/{studentId}/nfc",
            new IssueNfcCredentialRequest(card2Token, "Card #002"),
            JsonSerializationHelper.Options);

        issue2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var card2 = await issue2Response.Content.ReadFromJsonAsync<CredentialIssuedDto>(JsonSerializationHelper.Options);
        card2.Should().NotBeNull();
        card2!.ReplacedCredentialId.Should().Be(card1.CredentialId);

        // Verify in DB: Only card 2 is active, card 1 has revoked_at populated
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var c1FromDb = await db.StudentCredentials.FindAsync(card1.CredentialId);
        var c2FromDb = await db.StudentCredentials.FindAsync(card2.CredentialId);

        c1FromDb!.RevokedAt.Should().NotBeNull();
        c1FromDb.RevocationReason.Should().Contain("Replaced");
        c2FromDb!.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task IssueQr_ShouldReturnRawPayloadStartingWithTahdeeraUri()
    {
        // Arrange
        var (workspaceId, ownerClient, studentId) = await SetupWorkspaceWithStudentAsync();

        // Act
        var response = await ownerClient.PostAsJsonAsync<object?>(
            $"/api/workspaces/{workspaceId}/credentials/students/{studentId}/qr",
            null,
            JsonSerializationHelper.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var qrDto = await response.Content.ReadFromJsonAsync<QrCredentialIssuedDto>(JsonSerializationHelper.Options);
        qrDto.Should().NotBeNull();
        qrDto!.RawPayload.Should().StartWith("tahdeera://qr/");
        qrDto.RawPayload.Length.Should().Be(46); // tahdeera://qr/ (14 chars) + 32 hex = 46
    }

    [Fact]
    public async Task RevokeCredential_ShouldMarkRevoked_AndSecondRevocationShouldConflict()
    {
        // Arrange
        var (workspaceId, ownerClient, studentId) = await SetupWorkspaceWithStudentAsync();

        var issueResponse = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/credentials/students/{studentId}/nfc",
            new IssueNfcCredentialRequest("nfc_token_rev_" + Guid.NewGuid().ToString("N"), "Card #Rev"),
            JsonSerializationHelper.Options);
        var issued = await issueResponse.Content.ReadFromJsonAsync<CredentialIssuedDto>(JsonSerializationHelper.Options);

        // Act 1: Revoke first time
        var revoke1 = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/credentials/{issued!.CredentialId}/revoke",
            new RevokeCredentialRequest("Card was reported lost by guardian"),
            JsonSerializationHelper.Options);

        // Assert 1: Success
        revoke1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 2: Attempt duplicate revocation
        var revoke2 = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/credentials/{issued.CredentialId}/revoke",
            new RevokeCredentialRequest("Already lost"),
            JsonSerializationHelper.Options);

        // Assert 2: Conflict 409 CREDENTIAL_ALREADY_REVOKED
        revoke2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetStudentCredentials_WhenStudentNotEnrolledInWorkspace_ShouldReturnNotFound()
    {
        // Arrange
        var (workspaceId, ownerClient, _) = await SetupWorkspaceWithStudentAsync();
        var unenrolledStudentId = Guid.NewGuid();

        // Act
        var response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/credentials/students/{unenrolledStudentId}");

        // Assert: 404 Not Found
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task IssueCredential_WhenUserNotWorkspaceMember_ShouldReturnForbidden()
    {
        // Arrange: Workspace A and Workspace B
        var (workspaceA, _, studentIdA) = await SetupWorkspaceWithStudentAsync();
        var (_, clientB, _) = await SetupWorkspaceWithStudentAsync();

        // Act: User of Workspace B attempts to issue NFC credential in Workspace A
        var response = await clientB.PostAsJsonAsync(
            $"/api/workspaces/{workspaceA}/credentials/students/{studentIdA}/nfc",
            new IssueNfcCredentialRequest("unauthorized_token", "Card #X"),
            JsonSerializationHelper.Options);

        // Assert: 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
