using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.API.Security;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Common.Security;
using Tahdeera.Application.Features.Sessions.Commands.CancelSession;
using Tahdeera.Application.Features.Sessions.Commands.CompleteSession;
using Tahdeera.Application.Features.Sessions.Commands.ScheduleSession;
using Tahdeera.Application.Features.Sessions.Commands.UpdateSessionSchedule;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Application.Features.Sessions.Queries.GetGroupSessions;
using Tahdeera.Application.Features.Sessions.Queries.GetSessionDetails;
using Tahdeera.Domain.Enums;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
public class SessionsController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;

    public SessionsController(ISender sender, ICurrentUserService currentUserService)
    {
        _sender = sender;
        _currentUserService = currentUserService;
    }

    [HttpPost("api/workspaces/{workspaceId:guid}/groups/{groupId:guid}/sessions")]
    [HasPermission(Permissions.SessionsManage)]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScheduleSession(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid groupId,
        [FromBody] ScheduleSessionRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var command = new ScheduleSessionCommand(
            WorkspaceId: workspaceId,
            GroupId: groupId,
            StartsAt: request.StartsAt,
            EndsAt: request.EndsAt,
            LateGraceMinutes: request.LateGraceMinutes,
            RoomName: request.RoomName,
            Topic: request.Topic,
            ActorUserId: actorId
        );

        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet("api/workspaces/{workspaceId:guid}/groups/{groupId:guid}/sessions")]
    [HasPermission(Permissions.SessionsView)]
    [ProducesResponseType(typeof(PagedList<SessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupSessions(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid groupId,
        [FromQuery] SessionStatus? status = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetGroupSessionsQuery(
            WorkspaceId: workspaceId,
            GroupId: groupId,
            Status: status,
            FromDate: fromDate,
            ToDate: toDate,
            PageNumber: pageNumber,
            PageSize: pageSize
        );

        var result = await _sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("api/workspaces/{workspaceId:guid}/sessions/{id:guid}")]
    [HasPermission(Permissions.SessionsView)]
    [ProducesResponseType(typeof(SessionDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionDetails(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSessionDetailsQuery(workspaceId, id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("api/workspaces/{workspaceId:guid}/sessions/{id:guid}")]
    [HasPermission(Permissions.SessionsManage)]
    [ProducesResponseType(typeof(SessionScheduleUpdatedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSessionSchedule(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        [FromBody] UpdateSessionScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var command = new UpdateSessionScheduleCommand(
            WorkspaceId: workspaceId,
            SessionId: id,
            StartsAt: request.StartsAt,
            EndsAt: request.EndsAt,
            LateGraceMinutes: request.LateGraceMinutes,
            ActorUserId: actorId
        );

        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("api/workspaces/{workspaceId:guid}/sessions/{id:guid}/complete")]
    [HasPermission(Permissions.SessionsManage)]
    [ProducesResponseType(typeof(SessionCompletedSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteSession(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var command = new CompleteSessionCommand(workspaceId, id, actorId);
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("api/workspaces/{workspaceId:guid}/sessions/{id:guid}/cancel")]
    [HasPermission(Permissions.SessionsManage)]
    [ProducesResponseType(typeof(SessionCancelledDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSession(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        [FromBody] CancelSessionRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var command = new CancelSessionCommand(workspaceId, id, request.CancellationReason, actorId);
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
