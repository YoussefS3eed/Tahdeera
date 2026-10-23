using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.API.Security;
using Tahdeera.Application.Common.Security;
using Tahdeera.Application.Features.Finances.Commands.CancelStudentCharge;
using Tahdeera.Application.Features.Finances.Commands.CreateStudentCharge;
using Tahdeera.Application.Features.Finances.Commands.RecordStudentPayment;
using Tahdeera.Application.Features.Finances.Commands.ReverseStudentPayment;
using Tahdeera.Application.Features.Finances.Commands.WriteOffStudentCharge;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.Features.Finances.Queries.GetPrintableReceipt;
using Tahdeera.Application.Features.Finances.Queries.GetStudentAccountStatement;
using Tahdeera.Application.Features.Finances.Queries.GetStudentCharges;
using Tahdeera.Application.Features.Finances.Queries.GetStudentPayments;
using Tahdeera.Domain.Enums;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/students/{studentId:guid}/finance")]
[Route("api/v1/workspaces/{workspaceId:guid}/students/{studentId:guid}/finance")]
public class StudentFinanceController : ApiControllerBase
{
    private readonly ISender _sender;

    public StudentFinanceController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [HttpGet("account")]
    [HasPermission(Permissions.FinancesView)]
    [ProducesResponseType(typeof(StudentAccountStatementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountStatement(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        var query = new GetStudentAccountStatementQuery(workspaceId, studentId, fromDate, toDate);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("charges")]
    [HasPermission(Permissions.FinancesView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCharges(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        [FromQuery] ChargeStatus? status,
        [FromQuery] string? billingPeriod,
        CancellationToken cancellationToken)
    {
        var query = new GetStudentChargesQuery(workspaceId, studentId, status, billingPeriod);
        var charges = await _sender.Send(query, cancellationToken);
        return Ok(new { charges });
    }

    [HttpPost("charges")]
    [HasPermission(Permissions.FinancesManage)]
    [ProducesResponseType(typeof(StudentChargeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCharge(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        [FromBody] CreateStudentChargeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateStudentChargeCommand(
            WorkspaceId: workspaceId,
            StudentId: studentId,
            GroupId: request.GroupId,
            BillingPeriodLabel: request.BillingPeriodLabel,
            Description: request.Description,
            Amount: request.Amount,
            DueAt: request.DueAt,
            Notes: request.Notes);

        var result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("charges/{chargeId:guid}/cancel")]
    [HasPermission(Permissions.FinancesManage)]
    [ProducesResponseType(typeof(CancelChargeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelCharge(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        [FromRoute] Guid chargeId,
        [FromBody] CancelStudentChargeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CancelStudentChargeCommand(workspaceId, studentId, chargeId, request.Reason);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("charges/{chargeId:guid}/write-off")]
    [HasPermission(Permissions.FinancesManage)]
    [ProducesResponseType(typeof(WriteOffChargeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WriteOffCharge(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        [FromRoute] Guid chargeId,
        [FromBody] WriteOffStudentChargeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new WriteOffStudentChargeCommand(workspaceId, studentId, chargeId, request.Reason);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("payments")]
    [HasPermission(Permissions.FinancesView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayments(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        CancellationToken cancellationToken)
    {
        var query = new GetStudentPaymentsQuery(workspaceId, studentId);
        var payments = await _sender.Send(query, cancellationToken);
        return Ok(new { payments });
    }

    [HttpPost("payments")]
    [HasPermission(Permissions.FinancesManage)]
    [ProducesResponseType(typeof(RecordPaymentResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordPayment(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        [FromBody] RecordStudentPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordStudentPaymentCommand(
            WorkspaceId: workspaceId,
            StudentId: studentId,
            Amount: request.Amount,
            Method: request.Method,
            PaidAt: request.PaidAt,
            Reference: request.Reference,
            Notes: request.Notes,
            Allocations: request.Allocations);

        var result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("payments/{paymentId:guid}/reverse")]
    [HasPermission(Permissions.FinancesManage)]
    [ProducesResponseType(typeof(ReversePaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReversePayment(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        [FromRoute] Guid paymentId,
        [FromBody] ReverseStudentPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ReverseStudentPaymentCommand(workspaceId, studentId, paymentId, request.Reason);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("payments/{paymentId:guid}/receipt-print")]
    [HasPermission(Permissions.FinancesView)]
    [ProducesResponseType(typeof(PrintableReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrintableReceipt(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        [FromRoute] Guid paymentId,
        CancellationToken cancellationToken)
    {
        var query = new GetPrintableReceiptQuery(workspaceId, studentId, paymentId);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}