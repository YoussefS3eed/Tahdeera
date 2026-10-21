using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Tahdeera.Infrastructure.Configuration;

namespace Tahdeera.API.Security;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class MetaWebhookHmacAttribute : Attribute, IAsyncResourceFilter
{
    private const string SignatureHeader = "X-Hub-Signature-256";
    private readonly PlatformWhatsAppSettings _settings;
    private readonly ILogger<MetaWebhookHmacAttribute> _logger;

    public MetaWebhookHmacAttribute(
        IOptions<PlatformWhatsAppSettings> options,
        ILogger<MetaWebhookHmacAttribute> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        if (!request.Headers.TryGetValue(SignatureHeader, out var signatureHeaderValue) ||
            string.IsNullOrWhiteSpace(signatureHeaderValue))
        {
            _logger.LogWarning("Webhook request rejected: Missing {Header} header.", SignatureHeader);
            context.Result = CreateUnauthorizedResult(context.HttpContext, "Missing X-Hub-Signature-256 header.");
            return;
        }

        var headerString = signatureHeaderValue.ToString().Trim();
        if (!headerString.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Webhook request rejected: Malformed signature header format.");
            context.Result = CreateUnauthorizedResult(context.HttpContext, "Malformed signature header format. Must start with 'sha256='.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.AppSecret))
        {
            _logger.LogError("Webhook HMAC validation failed: PlatformWhatsApp:AppSecret is not configured.");
            context.Result = CreateUnauthorizedResult(context.HttpContext, "Server security configuration error.");
            return;
        }

        // Enable stream rewind so downstream JSON model binder can read the body
        request.EnableBuffering();

        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        byte[] bodyBytes;
        using (var memoryStream = new MemoryStream())
        {
            await request.Body.CopyToAsync(memoryStream);
            bodyBytes = memoryStream.ToArray();
            request.Body.Position = 0; // Rewind for model binder
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.AppSecret));
        var hashBytes = hmac.ComputeHash(bodyBytes);
        var computedSignature = "sha256=" + Convert.ToHexString(hashBytes).ToLowerInvariant();

        var computedBytes = Encoding.UTF8.GetBytes(computedSignature);
        var providedBytes = Encoding.UTF8.GetBytes(headerString);

        if (!CryptographicOperations.FixedTimeEquals(computedBytes, providedBytes))
        {
            _logger.LogWarning("Webhook request rejected: Invalid HMAC-SHA256 signature.");
            context.Result = CreateUnauthorizedResult(context.HttpContext, "Invalid HMAC-SHA256 signature.");
            return;
        }

        await next();
    }

    private static IActionResult CreateUnauthorizedResult(HttpContext context, string detail)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Invalid webhook cryptographic signature",
            Detail = detail,
            Type = "https://tahdeera.app/errors/webhook-signature-invalid",
            Instance = context.Request.Path
        };
        problem.Extensions["errorCode"] = "WEBHOOK_SIGNATURE_INVALID";
        problem.Extensions["traceId"] = traceId;

        return new UnauthorizedObjectResult(problem);
    }
}
