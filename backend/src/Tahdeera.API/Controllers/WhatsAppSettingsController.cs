namespace Tahdeera.API.Controllers;

using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.Application.Common.Security;
using Tahdeera.Application.Features.WhatsApp.Commands.ConnectWhatsAppSender;
using Tahdeera.Application.Features.WhatsApp.Commands.DisconnectWhatsAppSender;
using Tahdeera.Application.Features.WhatsApp.Commands.UpdateWorkspaceWhatsAppConfig;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppConfig;

[ApiVersion("1.0")]
[ApiController]
[Authorize(Policy = Permissions.WhatsAppManage)]
[Route("api/v1/workspaces/{workspaceId:guid}/whatsapp")]
[Route("api/workspaces/{workspaceId:guid}/whatsapp")]
public sealed class WhatsAppSettingsController : ApiControllerBase
{
    private readonly ISender _sender;

    public WhatsAppSettingsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(WorkspaceWhatsAppConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSettings(
        [FromRoute] Guid workspaceId,
        CancellationToken ct)
    {
        var query = new GetWorkspaceWhatsAppConfigQuery(workspaceId);
        var result = await _sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("settings")]
    [ProducesResponseType(typeof(WorkspaceWhatsAppConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSettings(
        [FromRoute] Guid workspaceId,
        [FromBody] UpdateWhatsAppSettingsRequest request,
        CancellationToken ct)
    {
        var command = new UpdateWorkspaceWhatsAppConfigCommand(
            workspaceId,
            request.SenderMode,
            request.IsNotificationsEnabled);

        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("connect")]
    [HttpPost("sender/connect")]
    [ProducesResponseType(typeof(WhatsAppSenderConnectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ConnectWaba(
        [FromRoute] Guid workspaceId,
        [FromBody] ConnectWhatsAppSenderRequest request,
        CancellationToken ct)
    {
        var command = new ConnectWhatsAppSenderCommand(
            workspaceId,
            request.Code,
            request.WabaId,
            request.PhoneNumberId,
            request.DisplayPhoneNumber);

        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("disconnect")]
    [HttpPost("sender/disconnect")]
    [HttpDelete("sender")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisconnectWaba(
        [FromRoute] Guid workspaceId,
        CancellationToken ct)
    {
        var command = new DisconnectWhatsAppSenderCommand(workspaceId);
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(new { message = "WhatsApp sender connection disconnected successfully." }) : HandleFailure(result);
    }
}
