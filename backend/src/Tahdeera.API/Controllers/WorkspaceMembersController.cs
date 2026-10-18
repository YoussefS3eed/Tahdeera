using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.Application.Features.Workspaces.Commands.AddWorkspaceMember;
using Tahdeera.Application.Features.Workspaces.Commands.DeactivateWorkspaceMember;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Application.Features.Workspaces.Queries.GetWorkspaceMembers;
using Tahdeera.Application.Features.Workspaces.Queries.GetWorkspaceSubscriptionStatus;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/members")]
public class WorkspaceMembersController : ApiControllerBase
{
    private readonly ISender _sender;

    public WorkspaceMembersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<WorkspaceMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMembers([FromRoute] Guid workspaceId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetWorkspaceMembersQuery(workspaceId), cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddWorkspaceMemberResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddMember(
        [FromRoute] Guid workspaceId,
        [FromBody] AddWorkspaceMemberRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddWorkspaceMemberCommand(
            workspaceId,
            request.Username,
            request.Email,
            request.Password,
            request.DisplayName,
            request.Phone,
            request.Role);

        var result = await _sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("{memberId:guid}/deactivate")]
    [ProducesResponseType(typeof(DeactivateWorkspaceMemberResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateMember(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid memberId,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateWorkspaceMemberCommand(workspaceId, memberId);
        var result = await _sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpGet("/api/workspaces/{workspaceId:guid}/subscription-status")]
    [ProducesResponseType(typeof(WorkspaceSubscriptionStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionStatus(
        [FromRoute] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetWorkspaceSubscriptionStatusQuery(workspaceId), cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        return Ok(result.Value);
    }
}
