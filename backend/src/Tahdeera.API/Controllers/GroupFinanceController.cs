using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.API.Security;
using Tahdeera.Application.Common.Security;
using Tahdeera.Application.Features.Finances.Commands.BatchCreateGroupCharges;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/groups/{groupId:guid}/finance")]
[Route("api/v1/workspaces/{workspaceId:guid}/groups/{groupId:guid}/finance")]
public class GroupFinanceController : ApiControllerBase
{
    private readonly ISender _sender;

    public GroupFinanceController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("billing-cycles")]
    [HasPermission(Permissions.FinancesManage)]
    [ProducesResponseType(typeof(BatchCreateGroupChargesResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BatchCreateGroupCharges(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid groupId,
        [FromBody] BatchCreateGroupChargesRequest request,
        CancellationToken cancellationToken)
    {
        var command = new BatchCreateGroupChargesCommand(
            WorkspaceId: workspaceId,
            GroupId: groupId,
            BillingPeriodLabel: request.BillingPeriodLabel,
            Description: request.Description,
            Amount: request.Amount,
            DueAt: request.DueAt,
            Notes: request.Notes);

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
