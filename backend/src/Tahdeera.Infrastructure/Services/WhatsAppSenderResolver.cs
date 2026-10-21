using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Application.Common.Models;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;
using Tahdeera.Infrastructure.Configuration;

namespace Tahdeera.Infrastructure.Services;

public sealed class WhatsAppSenderResolver : IWhatsAppSenderResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan IneligibleCacheDuration = TimeSpan.FromMinutes(1);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _memoryCache;
    private readonly IDataProtector _protector;
    private readonly PlatformWhatsAppSettings _platformSettings;
    private readonly ILogger<WhatsAppSenderResolver> _logger;

    public WhatsAppSenderResolver(
        IUnitOfWork unitOfWork,
        IMemoryCache memoryCache,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<PlatformWhatsAppSettings> platformOptions,
        ILogger<WhatsAppSenderResolver> logger)
    {
        _unitOfWork = unitOfWork;
        _memoryCache = memoryCache;
        _protector = dataProtectionProvider.CreateProtector("Tahdeera.WhatsApp.Tokens.v1");
        _platformSettings = platformOptions.Value;
        _logger = logger;
    }

    public async Task<WhatsAppSenderDescriptor?> ResolveSenderAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"wa_sender_{workspaceId}";

        if (_memoryCache.TryGetValue(cacheKey, out WhatsAppSenderDescriptor? cached) && cached is not null)
        {
            return cached;
        }

        // Query workspace config
        var config = await _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(workspaceId, cancellationToken);
        if (config is null || !config.NotificationsEnabled || config.SenderMode == WhatsAppSenderMode.Disabled)
        {
            var disabled = WhatsAppSenderDescriptor.Disabled(workspaceId);
            _memoryCache.Set(cacheKey, disabled, IneligibleCacheDuration);
            return disabled;
        }

        WhatsAppSenderDescriptor descriptor;

        if (config.SenderMode == WhatsAppSenderMode.WorkspaceOwned)
        {
            var activeConnection = await _unitOfWork.WhatsAppSenderConnections.GetActiveForWorkspaceAsync(workspaceId, cancellationToken);

            if (activeConnection is null || activeConnection.Status != SenderConnectionStatus.Connected)
            {
                // CONF-07: Strict Zero Silent Fallback - never route to platform credentials
                _logger.LogWarning("Workspace {WorkspaceId} is configured for WorkspaceOwned WhatsApp, but has no active connected WABA. Zero Silent Fallback enforced.", workspaceId);
                descriptor = WhatsAppSenderDescriptor.Unavailable(
                    workspaceId,
                    WhatsAppSenderMode.WorkspaceOwned,
                    "Workspace has no active, connected WhatsApp Business Account.");

                _memoryCache.Set(cacheKey, descriptor, IneligibleCacheDuration);
                return descriptor;
            }

            try
            {
                var decryptedToken = _protector.Unprotect(activeConnection.EncryptedAccessToken);
                descriptor = WhatsAppSenderDescriptor.Workspace(
                    workspaceId,
                    activeConnection.PhoneNumberId,
                    decryptedToken,
                    activeConnection.DisplayPhoneNumber,
                    activeConnection.BusinessDisplayName);

                _memoryCache.Set(cacheKey, descriptor, CacheDuration);
                return descriptor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt access token for WABA connection in workspace {WorkspaceId}", workspaceId);
                descriptor = WhatsAppSenderDescriptor.Unavailable(
                    workspaceId,
                    WhatsAppSenderMode.WorkspaceOwned,
                    "Failed to decrypt stored WhatsApp access token.");

                _memoryCache.Set(cacheKey, descriptor, IneligibleCacheDuration);
                return descriptor;
            }
        }
        else // PlatformManaged
        {
            if (!_platformSettings.IsEnabled || string.IsNullOrWhiteSpace(_platformSettings.PhoneNumberId))
            {
                _logger.LogWarning("Platform WhatsApp sender is not configured or disabled");
                descriptor = WhatsAppSenderDescriptor.Unavailable(
                    workspaceId,
                    WhatsAppSenderMode.PlatformManaged,
                    "Platform-managed WhatsApp sender is not configured or disabled.");

                _memoryCache.Set(cacheKey, descriptor, IneligibleCacheDuration);
                return descriptor;
            }

            descriptor = WhatsAppSenderDescriptor.Platform(
                workspaceId,
                _platformSettings.PhoneNumberId,
                _platformSettings.AccessToken,
                _platformSettings.PhoneNumberId,
                _platformSettings.BusinessDisplayName);

            _memoryCache.Set(cacheKey, descriptor, CacheDuration);
            return descriptor;
        }
    }

    public void InvalidateSenderCache(Guid workspaceId)
    {
        var cacheKey = $"wa_sender_{workspaceId}";
        _memoryCache.Remove(cacheKey);
        _logger.LogDebug("Evicted WhatsApp sender cache for workspace {WorkspaceId}", workspaceId);
    }
}
