using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Application.Features.WhatsApp.Commands.UpdateWorkspaceWhatsAppConfig;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;
using Xunit;

namespace Tahdeera.Application.UnitTests.WhatsApp;

public class UpdateWorkspaceWhatsAppConfigCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppSenderResolver _senderResolver;
    private readonly UpdateWorkspaceWhatsAppConfigCommandHandler _handler;

    public UpdateWorkspaceWhatsAppConfigCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _senderResolver = Substitute.For<IWhatsAppSenderResolver>();
        _handler = new UpdateWorkspaceWhatsAppConfigCommandHandler(_unitOfWork, _senderResolver);
    }

    [Fact]
    public async Task Handle_WhenSettingWorkspaceOwnedWithoutActiveConnection_ShouldFailWithSenderNotConfigured()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var command = new UpdateWorkspaceWhatsAppConfigCommand(workspaceId, "WorkspaceOwned", true);

        var config = WorkspaceWhatsAppConfig.Create(workspaceId);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);
        _unitOfWork.WhatsAppSenderConnections.GetActiveForWorkspaceAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns((WhatsAppSenderConnection?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SENDER_NOT_CONFIGURED");
    }

    [Fact]
    public async Task Handle_WhenDisablingNotifications_ShouldCancelPendingAndTransientOutboxMessagesAndInvalidateCache()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var command = new UpdateWorkspaceWhatsAppConfigCommand(workspaceId, "PlatformManaged", false);

        var config = WorkspaceWhatsAppConfig.Create(workspaceId);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsNotificationsEnabled.Should().BeFalse();
        await _unitOfWork.MessageOutbox.Received(1)
            .CancelPendingAndTransientForWorkspaceAsync(workspaceId, "Notifications disabled for workspace", Arg.Any<CancellationToken>());
        _senderResolver.Received(1).InvalidateSenderCache(workspaceId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSettingPlatformManaged_ShouldSucceedAndInvalidateCache()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var command = new UpdateWorkspaceWhatsAppConfigCommand(workspaceId, "PlatformManaged", true);

        var config = WorkspaceWhatsAppConfig.Create(workspaceId);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SenderMode.Should().Be("PlatformManaged");
        result.Value.IsNotificationsEnabled.Should().BeTrue();
        _senderResolver.Received(1).InvalidateSenderCache(workspaceId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
