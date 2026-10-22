using System.Text;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.API.Security;
using Tahdeera.Application.Common.Security;
using Tahdeera.Application.Features.Finances.Commands.StageTuitionReminders;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.Features.Finances.Queries.ExportWorkspaceFinanceCsv;
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

    [HttpPost("reminders")]
    [HasPermission(Permissions.FinancesManage)]
    [ProducesResponseType(typeof(StageTuitionRemindersResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StageReminders(
        [FromRoute] Guid workspaceId,
        [FromBody] StageTuitionRemindersRequest request,
        CancellationToken cancellationToken)
    {
        var command = new StageTuitionRemindersCommand(
            WorkspaceId: workspaceId,
            GroupId: request.GroupId,
            MinimumBalanceThreshold: request.MinimumBalanceThreshold);

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("export")]
    [HasPermission(Permissions.FinancesView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task ExportFinanceCsv(
        [FromRoute] Guid workspaceId,
        [FromQuery] DateTimeOffset fromDate,
        [FromQuery] DateTimeOffset toDate,
        [FromQuery] Guid? groupId,
        [FromQuery] Guid? cashierUserId,
        [FromQuery] string? paymentMethod,
        [FromQuery] string? transactionType,
        CancellationToken cancellationToken)
    {
        var query = new ExportWorkspaceFinanceCsvQuery(
            WorkspaceId: workspaceId,
            FromDate: fromDate,
            ToDate: toDate,
            GroupId: groupId,
            CashierUserId: cashierUserId,
            PaymentMethod: paymentMethod,
            TransactionType: transactionType);

        var rows = await _sender.Send(query, cancellationToken);

        Response.ContentType = "text/csv; charset=utf-8";
        var filename = $"tahdeera_finance_export_{DateTime.UtcNow:yyyyMMdd}.csv";
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{filename}\"");

        // Write UTF-8 BOM
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        await Response.Body.WriteAsync(bom, cancellationToken);

        await using var writer = new StreamWriter(Response.Body, new UTF8Encoding(false), leaveOpen: true);
        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            await writer.WriteAsync(row);
        }
        await writer.FlushAsync();
    }
}
