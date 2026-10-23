using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.Features.Finances.Queries.VerifyPrintableReceipt;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/finance")]
public class ReceiptVerificationController : ApiControllerBase
{
    private readonly ISender _sender;

    public ReceiptVerificationController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("verify-receipt/{receiptNumber}/{hash}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VerifiedReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyReceipt(
        [FromRoute] string receiptNumber,
        [FromRoute] string hash,
        CancellationToken cancellationToken)
    {
        var query = new VerifyPrintableReceiptQuery(receiptNumber, hash);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
