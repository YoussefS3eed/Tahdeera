using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Infrastructure.Configuration;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class WhatsAppUsageAndWebhookIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly string _appSecret;
    private readonly string _webhookVerifyToken;

    public WhatsAppUsageAndWebhookIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        using var scope = _factory.Services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<PlatformWhatsAppSettings>>().Value;
        _appSecret = settings.AppSecret;
        _webhookVerifyToken = settings.WebhookVerifyToken;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid WorkspaceId, HttpClient OwnerClient, string OwnerEmail, string OwnerPassword)> ProvisionWorkspaceAsync()
    {
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "TEACHER_STARTER");

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("wa_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("WhatsAppCenter"),
            Kind: "Teacher",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Alexandria, Egypt",
            DefaultLateGraceMinutes: 15,
            Owner: new CreateOwnerDto(
                Username: "wa_own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "WhatsApp Center Owner",
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

        return (wsResult.WorkspaceId, ownerClient, ownerEmail, ownerPassword);
    }

    // -------------------------------------------------------------
    // DEFECT 1 REMEDIATION & USAGE CONTROLLER TESTS
    // -------------------------------------------------------------

    [Fact]
    public async Task GetUsage_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var workspaceId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/workspaces/{workspaceId}/whatsapp/usage");

        // Assert: 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsage_WithoutMembershipOrPermission_ReturnsForbidden()
    {
        // Arrange: Workspace 1 provisioned
        var (workspace1Id, _, _, _) = await ProvisionWorkspaceAsync();

        // Workspace 2 provisioned with a different owner
        var (_, ws2OwnerClient, _, _) = await ProvisionWorkspaceAsync();

        // Act: Owner of Workspace 2 tries to access Workspace 1's WhatsApp usage
        ws2OwnerClient.DefaultRequestHeaders.Remove("X-Workspace-Id");
        ws2OwnerClient.DefaultRequestHeaders.Add("X-Workspace-Id", workspace1Id.ToString());

        var response = await ws2OwnerClient.GetAsync($"/api/workspaces/{workspace1Id}/whatsapp/usage");

        // Assert: 403 Forbidden (cross-tenant IDOR rejected by [HasPermission(Permissions.WhatsAppView)])
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsage_WithAuthorizedWorkspaceOwner_ReturnsOkWithUsageDetails()
    {
        // Arrange
        var (workspaceId, ownerClient, _, _) = await ProvisionWorkspaceAsync();

        // Act
        var response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/whatsapp/usage");

        // Assert: 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var usage = await response.Content.ReadFromJsonAsync<WorkspaceWhatsAppUsageDto>(JsonSerializationHelper.Options);
        usage.Should().NotBeNull();
        usage!.WorkspaceId.Should().Be(workspaceId);
        usage.MessagesDispatchedCount.Should().Be(0);
        usage.MessagesDeliveredCount.Should().Be(0);
        usage.MessagesFailedCount.Should().Be(0);
    }

    // -------------------------------------------------------------
    // WHATSAPP WEBHOOK HANDSHAKE TESTS
    // -------------------------------------------------------------

    [Fact]
    public async Task VerifyHandshake_ValidSubscribeQuery_ReturnsChallengeString()
    {
        // Arrange
        var client = _factory.CreateClient();
        var challenge = "random_challenge_" + Guid.NewGuid().ToString("N");
        var url = $"/api/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token={_webhookVerifyToken}&hub.challenge={challenge}";

        // Act
        var response = await client.GetAsync(url);

        // Assert: 200 OK with challenge in body
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be(challenge);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task VerifyHandshake_InvalidMode_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var url = $"/api/webhooks/whatsapp?hub.mode=unsupported_mode&hub.verify_token={_webhookVerifyToken}&hub.challenge=test";

        // Act
        var response = await client.GetAsync(url);

        // Assert: 400 Bad Request
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("WEBHOOK_MODE_INVALID");
    }

    [Fact]
    public async Task VerifyHandshake_InvalidVerifyToken_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        var url = $"/api/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=wrong_token_value&hub.challenge=test";

        // Act
        var response = await client.GetAsync(url);

        // Assert: 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("WEBHOOK_VERIFY_TOKEN_MISMATCH");
    }

    // -------------------------------------------------------------
    // WHATSAPP WEBHOOK HMAC SIGNATURE VALIDATION TESTS
    // -------------------------------------------------------------

    [Fact]
    public async Task ReceiveWebhook_MissingSignatureHeader_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new { @object = "whatsapp_business_account", entry = Array.Empty<object>() };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act (no X-Hub-Signature-256 header)
        var response = await client.PostAsync("/api/webhooks/whatsapp", content);

        // Assert: 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("WEBHOOK_SIGNATURE_INVALID");
    }

    [Fact]
    public async Task ReceiveWebhook_MalformedSignatureHeader_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new { @object = "whatsapp_business_account", entry = Array.Empty<object>() };
        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/whatsapp")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Hub-Signature-256", "sha1=abcdef1234567890");

        // Act
        var response = await client.SendAsync(request);

        // Assert: 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReceiveWebhook_InvalidSignatureHash_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new { @object = "whatsapp_business_account", entry = Array.Empty<object>() };
        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/whatsapp")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Hub-Signature-256", "sha256=0000000000000000000000000000000000000000000000000000000000000000");

        // Act
        var response = await client.SendAsync(request);

        // Assert: 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("WEBHOOK_SIGNATURE_INVALID");
    }

    [Fact]
    public async Task ReceiveWebhook_ValidHmacSignature_ProcessesWebhookAndReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new
        {
            @object = "whatsapp_business_account",
            entry = new[]
            {
                new
                {
                    id = "WHATSAPP_BUSINESS_ACCOUNT_ID",
                    changes = new[]
                    {
                        new
                        {
                            field = "messages",
                            value = new
                            {
                                messaging_product = "whatsapp",
                                metadata = new
                                {
                                    display_phone_number = "15550268580",
                                    phone_number_id = "100000000000001"
                                },
                                statuses = new[]
                                {
                                    new
                                    {
                                        id = "wamid.HBgLMTEwMDAwMDAwMQ==",
                                        status = "delivered",
                                        timestamp = "1729680000",
                                        recipient_id = "201012345678"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var bodyBytes = Encoding.UTF8.GetBytes(json);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_appSecret));
        var signatureHash = Convert.ToHexString(hmac.ComputeHash(bodyBytes)).ToLowerInvariant();
        var signatureHeader = $"sha256={signatureHash}";

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/whatsapp")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Hub-Signature-256", signatureHeader);

        // Act
        var response = await client.SendAsync(request);

        // Assert: 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("received");
    }
}
