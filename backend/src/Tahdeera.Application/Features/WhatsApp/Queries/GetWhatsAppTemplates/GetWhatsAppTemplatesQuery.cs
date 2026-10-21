namespace Tahdeera.Application.Features.WhatsApp.Queries.GetWhatsAppTemplates;

using MediatR;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;

public sealed record GetWhatsAppTemplatesQuery : IRequest<Result<WhatsAppTemplateCatalogResponseDto>>;
public sealed record GetWhatsAppTemplateByNameQuery(string TemplateName) : IRequest<Result<WhatsAppTemplateSummaryDto>>;
