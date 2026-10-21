namespace Tahdeera.Application.Common.Models.WhatsApp;

using System.Text.Json.Serialization;

public sealed record MetaOAuthTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType);
