using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Diagnostics;
using System.Text.Json;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Resources;

namespace Tahdeera.API.Middlewares;

public class SubscriptionLockoutMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public SubscriptionLockoutMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITahdeeraDbContext dbContext,
        IDateTimeProvider clock,
        IStringLocalizer<DomainMessages> localizer)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        // 1. Whitelist: auth, admin, health, preferences, subscription status check
        if (path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/preferences", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/subscription-status", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // 2. Extract Workspace ID from Route Data first, then Header
        Guid? workspaceId = null;
        var routeValues = context.GetRouteData()?.Values;
        if (routeValues != null &&
            routeValues.TryGetValue("workspaceId", out var routeVal) &&
            Guid.TryParse(routeVal?.ToString(), out var routeWsId))
        {
            workspaceId = routeWsId;
        }

        if (context.Request.Headers.TryGetValue("X-Workspace-Id", out var headerVal) &&
            Guid.TryParse(headerVal, out var headerWsId))
        {
            // Anti-spoofing check: route and header must agree if both provided
            if (workspaceId.HasValue && workspaceId.Value != headerWsId)
            {
                await WriteProblemDetailsResponseAsync(context, StatusCodes.Status400BadRequest,
                    "WORKSPACE_ID_MISMATCH", GetLocalizedMessage(localizer, "WORKSPACE_ID_MISMATCH", "معرف مساحة العمل في المسار لا يطابق الترويسة."));
                return;
            }
            workspaceId ??= headerWsId;
        }

        if (!workspaceId.HasValue)
        {
            await _next(context);
            return;
        }

        // 3. Filter mutating HTTP methods only (POST, PUT, PATCH, DELETE)
        var isMutation = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) ||
                         HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

        if (!isMutation)
        {
            await _next(context);
            return;
        }

        // 4. Query subscription state from PostgreSQL
        var subscription = await dbContext.WorkspaceSubscriptions
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId.Value)
            .OrderByDescending(s => s.EndsAt)
            .FirstOrDefaultAsync(context.RequestAborted);

        if (subscription != null)
        {
            var now = clock.UtcNow;

            if (subscription.ManualSuspendedAt != null)
            {
                await WriteProblemDetailsResponseAsync(context, StatusCodes.Status403Forbidden,
                    "WORKSPACE_SUSPENDED",
                    GetLocalizedMessage(localizer, "WORKSPACE_SUSPENDED", "تم إيقاف حساب السنتر إدارياً. يرجى التواصل مع إدارة المنصة (01272785618 / 01019482687)."),
                    subscription.SuspensionReason);
                return;
            }

            if (subscription.Status != "Active" || subscription.EndsAt <= now)
            {
                await WriteProblemDetailsResponseAsync(context, StatusCodes.Status403Forbidden,
                    "SUBSCRIPTION_EXPIRED",
                    GetLocalizedMessage(localizer, "SUBSCRIPTION_EXPIRED", "انتهت مدة اشتراكك في النظام. يرجى التواصل لتجديد الاشتراك (01272785618 / 01019482687)."),
                    expiryDate: subscription.EndsAt);
                return;
            }
        }

        await _next(context);
    }

    private static async Task WriteProblemDetailsResponseAsync(
        HttpContext context,
        int statusCode,
        string errorCode,
        string detail,
        string? suspensionReason = null,
        DateTimeOffset? expiryDate = null)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Type = $"https://tahdeera.app/errors/{errorCode.ToLowerInvariant().Replace('_', '-')}",
            Title = errorCode,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["supportPhone1"] = "01272785618";
        problemDetails.Extensions["supportPhone2"] = "01019482687";

        if (suspensionReason != null)
        {
            problemDetails.Extensions["suspensionReason"] = suspensionReason;
        }

        if (expiryDate.HasValue)
        {
            problemDetails.Extensions["expiryDate"] = expiryDate.Value.ToString("o");
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
