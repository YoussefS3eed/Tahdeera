using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Application.Common.Models.WhatsApp;
using Tahdeera.Application.Features.WhatsApp.Commands.ConnectWhatsAppSender;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests.WhatsApp;

public class ConnectWhatsAppSenderCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMetaOAuthClient _metaOAuthClient;
    private readonly IWhatsAppSenderResolver _senderResolver;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IDataProtector _protector;
    private readonly ConnectWhatsAppSenderCommandHandler _handler;

    public ConnectWhatsAppSenderCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _metaOAuthClient = Substitute.For<IMetaOAuthClient>();
        _senderResolver = Substitute.For<IWhatsAppSenderResolver>();
        _dataProtectionProvider = Substitute.For<IDataProtectionProvider>();
        _protector = Substitute.For<IDataProtector>();

        _dataProtectionProvider.CreateProtector(Arg.Any<string>()).Returns(_protector);
        _protector.Protect(Arg.Any<byte[]>()).Returns(c => c.Arg<byte[]>());

        _handler = new ConnectWhatsAppSenderCommandHandler(
            _unitOfWork,
            _metaOAuthClient,
            _senderResolver,
            _dataProtectionProvider);
    }

    [Fact]
    public async Task Handle_WhenOAuthExchangeFails_ShouldReturnMetaServiceUnavailable()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var command = new ConnectWhatsAppSenderCommand(workspaceId, "invalid_code", "12345", "67890", "+201012345678");

        _metaOAuthClient.ExchangeCodeForTokenAsync(command.Code, Arg.Any<CancellationToken>())
            .Returns((MetaOAuthTokenResponse?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("META_SERVICE_UNAVAILABLE");
    }

    [Fact]
    public async Task Handle_WhenOAuthExchangeSucceeds_ShouldEncryptTokenSaveConnectionAndEvictCache()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var command = new ConnectWhatsAppSenderCommand(workspaceId, "valid_code", "12345", "67890", "+201012345678");

        _metaOAuthClient.ExchangeCodeForTokenAsync(command.Code, Arg.Any<CancellationToken>())
            .Returns(new MetaOAuthTokenResponse("EAAG...", "Bearer"));

        var config = WorkspaceWhatsAppConfig.Create(workspaceId);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);
        _unitOfWork.WhatsAppSenderConnections.GetActiveForWorkspaceAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns((WhatsAppSenderConnection?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasAccessToken.Should().BeTrue();
        result.Value.WabaId.Should().Be("12345");
        result.Value.PhoneNumberId.Should().Be("67890");

        await _unitOfWork.WhatsAppSenderConnections.Received(1)
            .AddAsync(Arg.Any<WhatsAppSenderConnection>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _senderResolver.Received(1).InvalidateSenderCache(workspaceId);
    }
}
