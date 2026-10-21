namespace Tahdeera.Application.Common.Interfaces.Services;

using Tahdeera.Application.Common.Models.WhatsApp;

public interface IMetaOAuthClient
{
    Task<MetaOAuthTokenResponse?> ExchangeCodeForTokenAsync(
        string code,
        CancellationToken cancellationToken = default);
}
