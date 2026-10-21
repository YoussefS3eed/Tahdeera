using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppUsage;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/whatsapp/usage")]
public sealed class WhatsAppUsageController : ApiControllerBase
{
    private readonly ISender _sender;

    public WhatsAppUsageController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(WorkspaceWhatsAppUsageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsage(
        [FromRoute] Guid workspaceId,
        [FromQuery] string? yearMonth,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetWorkspaceWhatsAppUsageQuery(workspaceId, yearMonth), ct);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        return Ok(result.Value);
    }
}
