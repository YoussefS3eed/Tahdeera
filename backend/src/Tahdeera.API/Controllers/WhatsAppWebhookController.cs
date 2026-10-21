using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using Tahdeera.API.Security;
using Tahdeera.Application.Common.Models.Webhooks;
using Tahdeera.Application.Features.WhatsApp.Commands.ProcessWhatsAppWebhook;
using Tahdeera.Infrastructure.Configuration;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/webhooks/whatsapp")]
[AllowAnonymous]
public sealed class WhatsAppWebhookController : ControllerBase
{
    private readonly ISender _sender;
    private readonly PlatformWhatsAppSettings _settings;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        ISender sender,
        IOptions<PlatformWhatsAppSettings> options,
        ILogger<WhatsAppWebhookController> logger)
    {
        _sender = sender;
        _settings = options.Value;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult VerifyHandshake(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        if (mode != "subscribe")
        {
            _logger.LogWarning("Webhook handshake rejected: hub.mode '{Mode}' is invalid.", mode);
            var badModeProblem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid webhook mode",
                Detail = "hub.mode must equal 'subscribe'.",
                Type = "https://tahdeera.app/errors/webhook-mode-invalid",
                Instance = HttpContext.Request.Path
            };
            badModeProblem.Extensions["errorCode"] = "WEBHOOK_MODE_INVALID";
            badModeProblem.Extensions["traceId"] = traceId;
            return BadRequest(badModeProblem);
        }

        if (string.IsNullOrWhiteSpace(verifyToken) || verifyToken != _settings.WebhookVerifyToken)
        {
            _logger.LogWarning("Webhook handshake rejected: hub.verify_token mismatch.");
            var forbiddenProblem = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Webhook verification token mismatch",
                Detail = "The provided hub.verify_token does not match configured token.",
                Type = "https://tahdeera.app/errors/webhook-verify-token-mismatch",
                Instance = HttpContext.Request.Path
            };
            forbiddenProblem.Extensions["errorCode"] = "WEBHOOK_VERIFY_TOKEN_MISMATCH";
            forbiddenProblem.Extensions["traceId"] = traceId;
            return StatusCode(StatusCodes.Status403Forbidden, forbiddenProblem);
        }

        _logger.LogInformation("Meta WhatsApp webhook challenge successfully verified.");
        return Content(challenge ?? string.Empty, "text/plain", Encoding.UTF8);
    }

    [HttpPost]
    [ServiceFilter(typeof(MetaWebhookHmacAttribute))]
    public async Task<IActionResult> ReceiveWebhook(
        [FromBody] MetaWebhookPayload payload,
        CancellationToken ct)
    {
        await _sender.Send(new ProcessWhatsAppWebhookCommand(payload), ct);
        return Ok(new { status = "received" });
    }
}
