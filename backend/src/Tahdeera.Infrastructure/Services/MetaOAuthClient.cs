namespace Tahdeera.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Application.Common.Models.WhatsApp;
using Tahdeera.Infrastructure.Configuration;

public sealed class MetaOAuthClient : IMetaOAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly PlatformWhatsAppSettings _settings;
    private readonly ILogger<MetaOAuthClient> _logger;

    public MetaOAuthClient(
        HttpClient httpClient,
        IOptions<PlatformWhatsAppSettings> options,
        ILogger<MetaOAuthClient> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<MetaOAuthTokenResponse?> ExchangeCodeForTokenAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AppId) || string.IsNullOrWhiteSpace(_settings.AppSecret))
        {
            _logger.LogError("Cannot perform Meta OAuth exchange: PlatformWhatsApp AppId or AppSecret is not configured.");
            return null;
        }

        try
        {
            var uri = $"https://graph.facebook.com/v21.0/oauth/access_token?client_id={_settings.AppId}&client_secret={_settings.AppSecret}&code={Uri.EscapeDataString(code)}";
            var response = await _httpClient.GetAsync(uri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Meta OAuth token exchange failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<MetaOAuthTokenResponse>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception during Meta OAuth exchange.");
            return null;
        }
    }
}
