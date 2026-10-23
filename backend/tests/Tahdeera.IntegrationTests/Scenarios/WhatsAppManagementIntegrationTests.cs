using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class WhatsAppManagementIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public WhatsAppManagementIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid WorkspaceId, HttpClient OwnerClient)> ProvisionWorkspaceAsync()
    {
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "TEACHER_STARTER");

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("wa_mgmt_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("WhatsAppMgmtCenter"),
            Kind: "Teacher",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Giza, Egypt",
            DefaultLateGraceMinutes: 15,
            Owner: new CreateOwnerDto(
                Username: "mgmt_own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "WhatsApp Mgmt Owner",
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

        return (wsResult.WorkspaceId, ownerClient);
    }

    [Fact]
    public async Task GetSettings_AsWorkspaceOwner_ReturnsOkWithDefaultConfig()
    {
        // Arrange
        var (workspaceId, ownerClient) = await ProvisionWorkspaceAsync();

        // Act
        var response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/whatsapp/settings");

        // Assert: 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<WorkspaceWhatsAppConfigDto>(JsonSerializationHelper.Options);
        config.Should().NotBeNull();
        config!.WorkspaceId.Should().Be(workspaceId);
        config.SenderMode.Should().Be("PlatformManaged");
    }

    [Fact]
    public async Task UpdateSettings_AsWorkspaceOwner_PersistsNewSettings()
    {
        // Arrange
        var (workspaceId, ownerClient) = await ProvisionWorkspaceAsync();
        var request = new UpdateWhatsAppSettingsRequest(
            SenderMode: "PlatformManaged",
            IsNotificationsEnabled: true
        );

        // Act
        var response = await ownerClient.PutAsJsonAsync(
            $"/api/workspaces/{workspaceId}/whatsapp/settings",
            request,
            JsonSerializationHelper.Options);

        // Assert: 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<WorkspaceWhatsAppConfigDto>(JsonSerializationHelper.Options);
        updated.Should().NotBeNull();
        updated!.SenderMode.Should().Be("PlatformManaged");
        updated.IsNotificationsEnabled.Should().BeTrue();

        // Assert in Database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var dbConfig = await db.WorkspaceWhatsAppConfigs.FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId);
        dbConfig.Should().NotBeNull();
        dbConfig!.SenderMode.Should().Be(WhatsAppSenderMode.PlatformManaged);
        dbConfig.IsNotificationsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task ListTemplates_AsWorkspaceOwner_ReturnsCatalog()
    {
        // Arrange
        var (workspaceId, ownerClient) = await ProvisionWorkspaceAsync();

        // Act
        var response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/whatsapp/templates");

        // Assert: 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalog = await response.Content.ReadFromJsonAsync<WhatsAppTemplateCatalogResponseDto>(JsonSerializationHelper.Options);
        catalog.Should().NotBeNull();
        catalog!.Templates.Should().NotBeEmpty();
        catalog.Templates.Should().Contain(t => t.TemplateName == "attendance_check_in_v1");
        catalog.AllowedPlaceholders.Should().NotBeNull();
        catalog.AllowedPlaceholders.Ar.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTemplateByName_ExistingTemplate_ReturnsDetails()
    {
        // Arrange
        var (workspaceId, ownerClient) = await ProvisionWorkspaceAsync();

        // Act
        var response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/whatsapp/templates/attendance_check_in_v1");

        // Assert: 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var template = await response.Content.ReadFromJsonAsync<WhatsAppTemplateSummaryDto>(JsonSerializationHelper.Options);
        template.Should().NotBeNull();
        template!.TemplateName.Should().Be("attendance_check_in_v1");
        template.Category.Should().Be("UTILITY");
    }

    [Fact]
    public async Task GetTemplateByName_NonExistent_ReturnsNotFound()
    {
        // Arrange
        var (workspaceId, ownerClient) = await ProvisionWorkspaceAsync();

        // Act
        var response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/whatsapp/templates/unknown_template_123");

        // Assert: 404 NotFound
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListMessages_AsWorkspaceOwner_ReturnsPagedList()
    {
        // Arrange
        var (workspaceId, ownerClient) = await ProvisionWorkspaceAsync();

        // Act
        var response = await ownerClient.GetAsync($"/api/workspaces/{workspaceId}/whatsapp/messages?page=1&pageSize=10");

        // Assert: 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await response.Content.ReadFromJsonAsync<PagedList<WhatsAppMessageListItemDto>>(JsonSerializationHelper.Options);
        pagedList.Should().NotBeNull();
        pagedList!.PageNumber.Should().Be(1);
        pagedList.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task RetryMessage_NonExistent_ReturnsNotFound()
    {
        // Arrange
        var (workspaceId, ownerClient) = await ProvisionWorkspaceAsync();
        var fakeMessageId = Guid.NewGuid();

        // Act
        var response = await ownerClient.PostAsync(
            $"/api/workspaces/{workspaceId}/whatsapp/messages/{fakeMessageId}/retry",
            null);

        // Assert: 404 NotFound
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
