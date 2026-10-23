using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;
using Tahdeera.Infrastructure.Configuration;
using Tahdeera.Infrastructure.Services;

namespace Tahdeera.Application.UnitTests.Messaging;

public class WhatsAppSenderResolverTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _memoryCache;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IDataProtector _dataProtector;
    private readonly ILogger<WhatsAppSenderResolver> _logger;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public WhatsAppSenderResolverTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _dataProtectionProvider = new EphemeralDataProtectionProvider();
        _dataProtector = _dataProtectionProvider.CreateProtector("Tahdeera.WhatsApp.Tokens.v1");
        _logger = Substitute.For<ILogger<WhatsAppSenderResolver>>();
    }

    [Fact]
    public async Task ResolveSenderAsync_ConfigNullOrDisabled_ReturnsDisabledDescriptor()
    {
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns((WorkspaceWhatsAppConfig?)null);

        var platformOptions = Options.Create(new PlatformWhatsAppSettings { IsEnabled = false });
        var resolver = new WhatsAppSenderResolver(_unitOfWork, _memoryCache, _dataProtectionProvider, platformOptions, _logger);

        var result = await resolver.ResolveSenderAsync(_workspaceId);

        result.Should().NotBeNull();
        result!.CanSend.Should().BeFalse();
        result.SenderMode.Should().Be(WhatsAppSenderMode.Disabled);
    }

    [Fact]
    public async Task ResolveSenderAsync_PlatformManaged_ValidPlatformSettings_ReturnsPlatformSender()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.PlatformManaged);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        var platformOptions = Options.Create(new PlatformWhatsAppSettings
        {
            IsEnabled = true,
            PhoneNumberId = "platform_phone_123",
            AccessToken = "platform_access_token",
            BusinessDisplayName = "Tahdeera Central"
        });

        var resolver = new WhatsAppSenderResolver(_unitOfWork, _memoryCache, _dataProtectionProvider, platformOptions, _logger);

        var result = await resolver.ResolveSenderAsync(_workspaceId);

        result.Should().NotBeNull();
        result!.CanSend.Should().BeTrue();
        result.SenderMode.Should().Be(WhatsAppSenderMode.PlatformManaged);
        result.PhoneNumberId.Should().Be("platform_phone_123");
        result.AccessToken.Should().Be("platform_access_token");
        result.VerifiedBusinessName.Should().Be("Tahdeera Central");
    }

    [Fact]
    public async Task ResolveSenderAsync_PlatformManaged_DisabledInConfig_ReturnsUnavailable()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.PlatformManaged);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        var platformOptions = Options.Create(new PlatformWhatsAppSettings
        {
            IsEnabled = false,
            PhoneNumberId = string.Empty,
            AccessToken = string.Empty
        });

        var resolver = new WhatsAppSenderResolver(_unitOfWork, _memoryCache, _dataProtectionProvider, platformOptions, _logger);

        var result = await resolver.ResolveSenderAsync(_workspaceId);

        result.Should().NotBeNull();
        result!.CanSend.Should().BeFalse();
        result.RejectionReason.Should().Contain("Platform-managed WhatsApp sender is not configured or disabled");
    }

    [Fact]
    public async Task ResolveSenderAsync_WorkspaceOwned_SuccessfulDecryption_ReturnsWorkspaceSender()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.WorkspaceOwned);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        var realEncryptedToken = _dataProtector.Protect("decrypted_oauth_token_val");

        var connection = WhatsAppSenderConnection.CreateWorkspaceConnection(
            _workspaceId,
            "waba_999",
            "phone_id_555",
            "+201012345678",
            realEncryptedToken,
            "Teacher WABA");

        _unitOfWork.WhatsAppSenderConnections.GetActiveForWorkspaceAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var platformOptions = Options.Create(new PlatformWhatsAppSettings { IsEnabled = false });
        var resolver = new WhatsAppSenderResolver(_unitOfWork, _memoryCache, _dataProtectionProvider, platformOptions, _logger);

        var result = await resolver.ResolveSenderAsync(_workspaceId);

        result.Should().NotBeNull();
        result!.CanSend.Should().BeTrue();
        result.SenderMode.Should().Be(WhatsAppSenderMode.WorkspaceOwned);
        result.PhoneNumberId.Should().Be("phone_id_555");
        result.AccessToken.Should().Be("decrypted_oauth_token_val");
        result.DisplayPhoneNumber.Should().Be("+201012345678");
        result.VerifiedBusinessName.Should().Be("Teacher WABA");
    }

    [Fact]
    public async Task ResolveSenderAsync_WorkspaceOwned_DecryptionThrows_ReturnsUnavailableDescriptor()
    {
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.WorkspaceOwned);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        // A different protector from another key ring -> Unprotect will fail with CryptographicException!
        var foreignProvider = new EphemeralDataProtectionProvider();
        var foreignProtector = foreignProvider.CreateProtector("Different.Purpose");
        var foreignEncryptedToken = foreignProtector.Protect("some_token");

        var connection = WhatsAppSenderConnection.CreateWorkspaceConnection(
            _workspaceId,
            "waba_999",
            "phone_id_555",
            "+201012345678",
            foreignEncryptedToken,
            "Teacher WABA");

        _unitOfWork.WhatsAppSenderConnections.GetActiveForWorkspaceAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var platformOptions = Options.Create(new PlatformWhatsAppSettings { IsEnabled = false });
        var resolver = new WhatsAppSenderResolver(_unitOfWork, _memoryCache, _dataProtectionProvider, platformOptions, _logger);

        var result = await resolver.ResolveSenderAsync(_workspaceId);

        result.Should().NotBeNull();
        result!.CanSend.Should().BeFalse();
        result.RejectionReason.Should().Be("Failed to decrypt stored WhatsApp access token.");
    }
}
