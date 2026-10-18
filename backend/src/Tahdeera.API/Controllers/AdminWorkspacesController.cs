using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Application.Features.Workspaces.Queries.GetAdminWorkspaces;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[Route("api/admin/workspaces")]
[Authorize(Policy = "PlatformAdmin")]
public class AdminWorkspacesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AdminWorkspacesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateWorkspaceResultDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : HandleFailure(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<AdminWorkspaceListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkspaces(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var result = await _mediator.Send(new GetAdminWorkspacesQuery(page, pageSize, search, status));
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
