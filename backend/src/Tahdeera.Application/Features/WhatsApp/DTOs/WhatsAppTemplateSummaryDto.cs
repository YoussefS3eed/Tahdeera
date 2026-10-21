namespace Tahdeera.Application.Features.WhatsApp.DTOs;

public sealed record WhatsAppTemplateSummaryDto(
    string TemplateName,
    string Category,
    IReadOnlyList<string> SupportedLanguages,
    IReadOnlyList<string> Placeholders,
    string PreviewAr,
    string PreviewEn);

public sealed record WhatsAppTemplateCatalogResponseDto(
    IReadOnlyList<WhatsAppTemplateSummaryDto> Templates,
    AllowedPlaceholdersDto AllowedPlaceholders);

public sealed record AllowedPlaceholdersDto(
    IReadOnlyList<string> Ar,
    IReadOnlyList<string> En);
