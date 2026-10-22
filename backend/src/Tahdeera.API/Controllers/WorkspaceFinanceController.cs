using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.API.Security;
using Tahdeera.Application.Common.Security;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceSummary;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/finance")]
[Route("api/v1/workspaces/{workspaceId:guid}/finance")]
public class WorkspaceFinanceController : ApiControllerBase
{
    private readonly ISender _sender;

    public WorkspaceFinanceController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [HttpGet("summary")]
    [HasPermission(Permissions.FinancesView)]
    [ProducesResponseType(typeof(WorkspaceFinanceSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary(
        [FromRoute] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var query = new GetWorkspaceFinanceSummaryQuery(workspaceId);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
