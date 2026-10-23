using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Domain.UnitTests.Entities;

public class WorkspaceWhatsAppConfigTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();

    [Fact]
    public void Create_InitializesDefaultQuotaAndMode()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId);

        config.WorkspaceId.Should().Be(_workspaceId);
        config.SenderMode.Should().Be(WhatsAppSenderMode.PlatformManaged);
        config.MonthlyMessageQuota.Should().Be(1000);
        config.MessagesUsedThisMonth.Should().Be(0);
        config.IsNotificationsEnabled.Should().BeTrue();
        config.NotificationsEnabled.Should().BeTrue();
        config.ActiveSenderConnectionId.Should().BeNull();
    }

    [Fact]
    public void UpdateSenderMode_ToWorkspaceOwnedWithoutConnection_ThrowsException()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId);

        var act = () => config.UpdateSenderMode(WhatsAppSenderMode.WorkspaceOwned);

        var ex = act.Should().Throw<DomainValidationException>().Which;
        ex.ErrorCode.Should().Be("SENDER_NOT_CONFIGURED");
    }

    [Fact]
    public void UpdateSenderMode_ToWorkspaceOwnedWithConnection_Succeeds()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId);
        var connectionId = Guid.NewGuid();

        config.UpdateSenderMode(WhatsAppSenderMode.WorkspaceOwned, connectionId);

        config.SenderMode.Should().Be(WhatsAppSenderMode.WorkspaceOwned);
        config.ActiveSenderConnectionId.Should().Be(connectionId);
    }

    [Fact]
    public void IncrementUsage_IncrementsMessagesUsedCount()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId);

        config.IncrementUsage();
        config.IncrementUsage();

        config.MessagesUsedThisMonth.Should().Be(2);

        config.ResetMonthlyUsage();
        config.MessagesUsedThisMonth.Should().Be(0);
    }

    [Fact]
    public void SetMonthlyQuota_WithNegativeValue_ThrowsDomainValidationException()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId);

        var act = () => config.SetMonthlyQuota(-5);

        var ex = act.Should().Throw<DomainValidationException>().Which;
        ex.ErrorCode.Should().Be("INVALID_QUOTA");
    }

    [Fact]
    public void SetMonthlyQuota_WithValidValue_UpdatesMonthlyMessageQuota()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId);

        config.SetMonthlyQuota(5000);

        config.MonthlyMessageQuota.Should().Be(5000);
    }

    [Fact]
    public void ToggleNotifications_UpdatesStatusAndComputedProperty()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId);
        config.NotificationsEnabled.Should().BeTrue();

        config.ToggleNotifications(false);

        config.IsNotificationsEnabled.Should().BeFalse();
        config.NotificationsEnabled.Should().BeFalse();
    }

    [Fact]
    public void AssignActiveConnection_SetsConnectionId()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId);
        var connectionId = Guid.NewGuid();

        config.AssignActiveConnection(connectionId);

        config.ActiveSenderConnectionId.Should().Be(connectionId);
    }

    [Fact]
    public void ClearActiveConnection_WhenWorkspaceOwned_ResetsSenderModeToDisabled()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId);
        var connectionId = Guid.NewGuid();
        config.UpdateSenderMode(WhatsAppSenderMode.WorkspaceOwned, connectionId);

        config.ClearActiveConnection();

        config.ActiveSenderConnectionId.Should().BeNull();
        config.SenderMode.Should().Be(WhatsAppSenderMode.Disabled);
    }

    [Fact]
    public void UpdateSenderMode_ToPlatformManaged_ClearsActiveSenderConnectionId()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId);
        var connectionId = Guid.NewGuid();
        config.UpdateSenderMode(WhatsAppSenderMode.WorkspaceOwned, connectionId);

        config.UpdateSenderMode(WhatsAppSenderMode.PlatformManaged);

        config.SenderMode.Should().Be(WhatsAppSenderMode.PlatformManaged);
        config.ActiveSenderConnectionId.Should().BeNull();
    }
}
