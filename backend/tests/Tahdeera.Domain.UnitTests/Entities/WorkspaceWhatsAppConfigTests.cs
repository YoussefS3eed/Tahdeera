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
}
