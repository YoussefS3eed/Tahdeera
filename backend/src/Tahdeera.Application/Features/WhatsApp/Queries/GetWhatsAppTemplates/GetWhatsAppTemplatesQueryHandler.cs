namespace Tahdeera.Application.Features.WhatsApp.Queries.GetWhatsAppTemplates;

using MediatR;
using Tahdeera.Application.Common.Models.WhatsApp;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;

public sealed class GetWhatsAppTemplatesQueryHandler
    : IRequestHandler<GetWhatsAppTemplatesQuery, Result<WhatsAppTemplateCatalogResponseDto>>,
      IRequestHandler<GetWhatsAppTemplateByNameQuery, Result<WhatsAppTemplateSummaryDto>>
{
    public Task<Result<WhatsAppTemplateCatalogResponseDto>> Handle(
        GetWhatsAppTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var summaries = WhatsAppTemplateRegistry.Catalog.Values
            .Select(t => new WhatsAppTemplateSummaryDto(
                t.TemplateName,
                t.Category,
                t.SupportedLanguages,
                t.Placeholders,
                t.PreviewAr,
                t.PreviewEn))
            .ToList();

        var allowed = new AllowedPlaceholdersDto(
            WhatsAppTemplateRegistry.AllowedPlaceholdersAr,
            WhatsAppTemplateRegistry.AllowedPlaceholdersEn);

        return Task.FromResult(Result.Success(new WhatsAppTemplateCatalogResponseDto(summaries, allowed)));
    }

    public Task<Result<WhatsAppTemplateSummaryDto>> Handle(
        GetWhatsAppTemplateByNameQuery request,
        CancellationToken cancellationToken)
    {
        if (!WhatsAppTemplateRegistry.Catalog.TryGetValue(request.TemplateName, out var t))
        {
            return Task.FromResult(Result.Failure<WhatsAppTemplateSummaryDto>(DomainErrors.WhatsApp.TemplateNotFound));
        }

        var dto = new WhatsAppTemplateSummaryDto(
            t.TemplateName,
            t.Category,
            t.SupportedLanguages,
            t.Placeholders,
            t.PreviewAr,
            t.PreviewEn);

        return Task.FromResult(Result.Success(dto));
    }
}
