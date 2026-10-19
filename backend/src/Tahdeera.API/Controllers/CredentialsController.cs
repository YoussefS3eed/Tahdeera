using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.API.Security;
using Tahdeera.Application.Common.Security;
using Tahdeera.Application.Features.Credentials.Commands.IssueNfcCredential;
using Tahdeera.Application.Features.Credentials.Commands.IssueQrCredential;
using Tahdeera.Application.Features.Credentials.Commands.RevokeCredential;
using Tahdeera.Application.Features.Credentials.DTOs;
using Tahdeera.Application.Features.Credentials.Queries.GetStudentCredentials;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/credentials")]
public class CredentialsController : ApiControllerBase
{
    private readonly ISender _sender;

    public CredentialsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("students/{studentId:guid}/nfc")]
    [HasPermission(Permissions.CredentialsManage)]
    [ProducesResponseType(typeof(CredentialIssuedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> IssueNfc(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        [FromBody] IssueNfcCredentialRequest request,
        CancellationToken cancellationToken)
    {
        var command = new IssueNfcCredentialCommand(
            WorkspaceId: workspaceId,
            StudentId: studentId,
            Token: request.Token,
            CardUidDisplay: request.CardUidDisplay
        );

        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("students/{studentId:guid}/qr")]
    [HasPermission(Permissions.CredentialsManage)]
    [ProducesResponseType(typeof(QrCredentialIssuedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IssueQr(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        CancellationToken cancellationToken)
    {
        var command = new IssueQrCredentialCommand(
            WorkspaceId: workspaceId,
            StudentId: studentId
        );

        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("{credentialId:guid}/revoke")]
    [HasPermission(Permissions.CredentialsManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevokeCredential(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid credentialId,
        [FromBody] RevokeCredentialRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RevokeCredentialCommand(
            WorkspaceId: workspaceId,
            CredentialId: credentialId,
            Reason: request.Reason
        );

        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok() : HandleFailure(result);
    }

    [HttpGet("students/{studentId:guid}")]
    [HasPermission(Permissions.CredentialsManage)]
    [ProducesResponseType(typeof(List<CredentialDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudentCredentials(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetStudentCredentialsQuery(workspaceId, studentId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
