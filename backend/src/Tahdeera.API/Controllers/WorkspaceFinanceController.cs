using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.API.Security;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Common.Security;
using Tahdeera.Application.Features.Finances.Commands.RecordStudentPayment;
using Tahdeera.Application.Features.Finances.Commands.StageTuitionReminders;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.Features.Finances.Queries.ExportWorkspaceFinanceCsv;
using Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceLedger;
using Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceStudents;
using Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceSummary;
using Tahdeera.Domain.Enums;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/finance")]
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

    [HttpGet("students")]
    [HasPermission(Permissions.FinancesView)]
    [ProducesResponseType(typeof(PagedResult<WorkspaceFinanceStudentItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudents(
        [FromRoute] Guid workspaceId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? groupId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetWorkspaceFinanceStudentsQuery(
            WorkspaceId: workspaceId,
            PageIndex: pageIndex,
            PageSize: pageSize,
            Status: status,
            Search: search,
            GroupId: groupId);

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("ledger")]
    [HasPermission(Permissions.FinancesView)]
    [ProducesResponseType(typeof(PagedResult<WorkspaceFinanceLedgerItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLedger(
        [FromRoute] Guid workspaceId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] string? status = null,
        [FromQuery] string? transactionType = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetWorkspaceFinanceLedgerQuery(
            WorkspaceId: workspaceId,
            PageIndex: pageIndex,
            PageSize: pageSize,
            FromDate: fromDate,
            ToDate: toDate,
            Status: status,
            TransactionType: transactionType);

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("payments")]
    [HttpPost("/api/workspaces/{workspaceId:guid}/payments")]
    [HasPermission(Permissions.FinancesManage)]
    [ProducesResponseType(typeof(RecordPaymentResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordWorkspacePayment(
        [FromRoute] Guid workspaceId,
        [FromBody] RecordWorkspacePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var methodEnum))
        {
            methodEnum = PaymentMethod.Cash;
        }

        var allocations = request.Allocations?
            .Select(a => new PaymentAllocationRequestDto(a.ChargeId, a.Amount))
            .ToList();

        var command = new RecordStudentPaymentCommand(
            WorkspaceId: workspaceId,
            StudentId: request.StudentId,
            Amount: request.Amount,
            Method: methodEnum,
            PaidAt: request.PaidAt ?? DateTimeOffset.UtcNow,
            Reference: request.TransactionReference,
            Notes: request.Notes,
            Allocations: allocations);

        var result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
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
