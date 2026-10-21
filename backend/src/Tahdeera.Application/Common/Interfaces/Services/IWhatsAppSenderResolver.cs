namespace Tahdeera.Application.Common.Interfaces.Services;

using Tahdeera.Application.Common.Models;

/// <summary>
/// Resolves and decrypts active WhatsApp sender credentials per workspace.
/// Enforces Zero Silent Fallback (CONF-07) and short-term memory caching.
/// </summary>
public interface IWhatsAppSenderResolver
{
    /// <summary>
    /// Resolves the sender credentials for the specified workspace.
    /// Checks IMemoryCache first; falls back to database lookup and DataProtection decryption.
    /// </summary>
    Task<WhatsAppSenderDescriptor?> ResolveSenderAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evicts the cached sender descriptor for the specified workspace immediately.
    /// Called when workspace credentials, tokens, or sender modes are updated or disconnected.
    /// </summary>
    void InvalidateSenderCache(Guid workspaceId);
}
