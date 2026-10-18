using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Tahdeera.Application.Common.Exceptions;
using Tahdeera.Application.Resources;
using Tahdeera.Domain.Common;

namespace Tahdeera.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IStringLocalizer<DomainMessages> localizer)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, localizer);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IStringLocalizer<DomainMessages> localizer)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var (statusCode, errorCode, title, detail, errors) = exception switch
        {
            ValidationException valEx => (
                (int)HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                GetLocalizedMessage(localizer, "VALIDATION_ERROR", "Validation Error"),
                valEx.Message,
                valEx.Errors
            ),
            DomainException domEx => (
                (int)domEx.StatusCode,
                domEx.ErrorCode,
                GetLocalizedMessage(localizer, domEx.ErrorCode, "Domain Error"),
                GetLocalizedMessage(localizer, domEx.ErrorCode, domEx.Message),
                (IDictionary<string, string[]>?)null
            ),
            UnauthorizedException unauthEx => (
                (int)HttpStatusCode.Unauthorized,
                unauthEx.ErrorCode,
                GetLocalizedMessage(localizer, unauthEx.ErrorCode, "Unauthorized"),
                GetLocalizedMessage(localizer, unauthEx.ErrorCode, unauthEx.Message),
                (IDictionary<string, string[]>?)null
            ),
            ForbiddenException forbidEx => (
                (int)HttpStatusCode.Forbidden,
                forbidEx.ErrorCode,
                GetLocalizedMessage(localizer, forbidEx.ErrorCode, "Forbidden"),
                GetLocalizedMessage(localizer, forbidEx.ErrorCode, forbidEx.Message),
                (IDictionary<string, string[]>?)null
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "INTERNAL_SERVER_ERROR",
                GetLocalizedMessage(localizer, "INTERNAL_SERVER_ERROR", "Internal Server Error"),
                GetLocalizedMessage(localizer, "INTERNAL_SERVER_ERROR", "An unexpected error occurred, please try again later."),
                (IDictionary<string, string[]>?)null
            )
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message} [TraceId: {TraceId}]", exception.Message, traceId);
        }
        else
        {
            _logger.LogWarning("Handled application exception: {ErrorCode} - {Detail} [TraceId: {TraceId}]", errorCode, detail, traceId);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Type = $"https://tahdeera.app/errors/{errorCode.ToLowerInvariant().Replace('_', '-')}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = traceId;

        if (errors != null && errors.Count != 0)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        var responseJson = JsonSerializer.Serialize(problemDetails, JsonOptions);
        await context.Response.WriteAsync(responseJson);
    }

    private static string GetLocalizedMessage(IStringLocalizer<DomainMessages> localizer, string key, string fallback)
    {
        var localizedString = localizer[key];
        return !localizedString.ResourceNotFound ? localizedString.Value : fallback;
    }
}
