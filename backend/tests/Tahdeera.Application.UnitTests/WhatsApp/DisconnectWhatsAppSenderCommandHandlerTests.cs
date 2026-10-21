using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Application.Features.WhatsApp.Commands.DisconnectWhatsAppSender;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;
using Xunit;

namespace Tahdeera.Application.UnitTests.WhatsApp;

public class DisconnectWhatsAppSenderCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppSenderResolver _senderResolver;
    private readonly DisconnectWhatsAppSenderCommandHandler _handler;

    public DisconnectWhatsAppSenderCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _senderResolver = Substitute.For<IWhatsAppSenderResolver>();
        _handler = new DisconnectWhatsAppSenderCommandHandler(_unitOfWork, _senderResolver);
    }

    [Fact]
    public async Task Handle_WhenNoActiveConnection_ShouldFailWithWabaConnectionNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var command = new DisconnectWhatsAppSenderCommand(workspaceId);

        _unitOfWork.WhatsAppSenderConnections.GetActiveForWorkspaceAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns((WhatsAppSenderConnection?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WABA_CONNECTION_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WhenActiveConnectionExists_ShouldMarkDisconnectedCancelOutboxAndEvictCache()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var command = new DisconnectWhatsAppSenderCommand(workspaceId);

        var connection = WhatsAppSenderConnection.CreateWorkspaceConnection(
            workspaceId, "12345", "67890", "+201012345678", "encToken", verifiedBusinessName: null);

        var config = WorkspaceWhatsAppConfig.Create(workspaceId, WhatsAppSenderMode.WorkspaceOwned);
        config.AssignActiveConnection(connection.Id);

        _unitOfWork.WhatsAppSenderConnections.GetActiveForWorkspaceAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(connection);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        connection.Status.Should().Be(SenderConnectionStatus.Disconnected);
        config.ActiveSenderConnectionId.Should().BeNull();

        await _unitOfWork.MessageOutbox.Received(1)
            .CancelPendingAndTransientForWorkspaceAsync(workspaceId, "WhatsApp sender disconnected by center owner", Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _senderResolver.Received(1).InvalidateSenderCache(workspaceId);
    }
}
