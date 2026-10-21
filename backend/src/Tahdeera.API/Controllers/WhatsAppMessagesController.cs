namespace Tahdeera.API.Controllers;

using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Common.Security;
using Tahdeera.Application.Features.WhatsApp.Commands.RetryWhatsAppMessage;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Application.Features.WhatsApp.Queries.GetWhatsAppMessageById;
using Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppMessages;
using Tahdeera.Domain.Repositories;

[ApiVersion("1.0")]
[ApiController]
[Authorize(Policy = Permissions.WhatsAppView)]
[Route("api/v1/workspaces/{workspaceId:guid}/whatsapp/messages")]
[Route("api/workspaces/{workspaceId:guid}/whatsapp/messages")]
public sealed class WhatsAppMessagesController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public WhatsAppMessagesController(
        ISender sender,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _sender = sender;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedList<WhatsAppMessageListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListMessages(
        [FromRoute] Guid workspaceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? messageType = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken ct = default)
    {
        bool isOwner = await CheckIsOwnerAsync(workspaceId, ct);

        var query = new GetWorkspaceWhatsAppMessagesQuery(
            workspaceId,
            isOwner,
            page,
            pageSize,
            status,
            messageType,
            fromDate,
            toDate);

        var result = await _sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WhatsAppMessageDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageById(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        bool isOwner = await CheckIsOwnerAsync(workspaceId, ct);

        var query = new GetWhatsAppMessageByIdQuery(workspaceId, id, isOwner);
        var result = await _sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(typeof(RetryWhatsAppMessageResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RetryMessage(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var command = new RetryWhatsAppMessageCommand(workspaceId, id);
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    private async Task<bool> CheckIsOwnerAsync(Guid workspaceId, CancellationToken ct)
    {
        if (User.IsInRole("PlatformAdmin") || User.IsInRole("SuperAdmin"))
        {
            return true;
        }

        if (_currentUserService.UserId.HasValue)
        {
            var member = await _unitOfWork.WorkspaceMembers.FirstOrDefaultAsync(
                m => m.WorkspaceId == workspaceId && m.UserId == _currentUserService.UserId.Value && m.IsActive, ct);
            return member?.Role == "Owner";
        }

        return false;
    }
}
