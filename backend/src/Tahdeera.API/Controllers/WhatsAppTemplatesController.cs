namespace Tahdeera.API.Controllers;

using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.Application.Common.Security;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Application.Features.WhatsApp.Queries.GetWhatsAppTemplates;

[ApiVersion("1.0")]
[ApiController]
[Authorize(Policy = Permissions.WhatsAppView)]
[Route("api/workspaces/{workspaceId:guid}/whatsapp/templates")]
[Route("api/whatsapp/templates")]
public sealed class WhatsAppTemplatesController : ApiControllerBase
{
    private readonly ISender _sender;

    public WhatsAppTemplatesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(WhatsAppTemplateCatalogResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListTemplates(
        [FromRoute] Guid? workspaceId,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetWhatsAppTemplatesQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{templateName}")]
    [ProducesResponseType(typeof(WhatsAppTemplateSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplateByName(
        [FromRoute] Guid? workspaceId,
        [FromRoute] string templateName,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetWhatsAppTemplateByNameQuery(templateName), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
