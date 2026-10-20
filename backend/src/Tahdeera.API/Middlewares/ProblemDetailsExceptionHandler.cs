using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Diagnostics;
using System.Net;
using Tahdeera.Application.Common.Exceptions;
using Tahdeera.Application.Resources;
using Tahdeera.Domain.Common;

namespace Tahdeera.API.Middlewares;

/// <summary>
/// Native ASP.NET Core 10 IExceptionHandler implementation for formatting unhandled exceptions
/// as RFC 9457 ProblemDetails with bilingual Arabic/English message resolution.
/// </summary>
public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;
    private readonly IStringLocalizer<DomainMessages> _localizer;

    public ProblemDetailsExceptionHandler(
        ILogger<ProblemDetailsExceptionHandler> logger,
        IStringLocalizer<DomainMessages> localizer)
    {
        _logger = logger;
        _localizer = localizer;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var (statusCode, errorCode, title, detail) = exception switch
        {
            ValidationException valEx => (
                (int)HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                GetLocalized("VALIDATION_ERROR", "Validation Error"),
                valEx.Message
            ),
            DomainException domEx => (
                (int)domEx.StatusCode,
                domEx.ErrorCode,
                GetLocalized(domEx.ErrorCode, "Domain Error"),
                GetLocalized(domEx.ErrorCode, domEx.Message)
            ),
            UnauthorizedException unauthEx => (
                (int)HttpStatusCode.Unauthorized,
                unauthEx.ErrorCode,
                GetLocalized(unauthEx.ErrorCode, "Unauthorized"),
                GetLocalized(unauthEx.ErrorCode, unauthEx.Message)
            ),
            ForbiddenException forbidEx => (
                (int)HttpStatusCode.Forbidden,
                forbidEx.ErrorCode,
                GetLocalized(forbidEx.ErrorCode, "Forbidden"),
                GetLocalized(forbidEx.ErrorCode, forbidEx.Message)
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "INTERNAL_SERVER_ERROR",
                GetLocalized("INTERNAL_SERVER_ERROR", "Internal Server Error"),
                GetLocalized("INTERNAL_SERVER_ERROR", "An unexpected error occurred, please try again later.")
            )
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled server error: {Message} [TraceId: {TraceId}]", exception.Message, traceId);
        }
        else
        {
            _logger.LogWarning("Handled application error: {ErrorCode} - {Detail} [TraceId: {TraceId}]", errorCode, detail, traceId);
        }

        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Type = $"https://tahdeera.app/errors/{errorCode.ToLowerInvariant().Replace('_', '-')}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = traceId;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private string GetLocalized(string key, string fallback)
    {
        var localized = _localizer[key];
        return (!localized.ResourceNotFound && !string.IsNullOrWhiteSpace(localized.Value))
            ? localized.Value
            : fallback;
    }
}
