using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Tahdeera.Application.Resources;

namespace Tahdeera.API.Extensions;

public static class RateLimitingExtensions
{
    public const string PolicyName = "StudentsAntiEnumerationRateLimitPolicy";
    public const string PublicReceiptVerificationPolicy = "PublicReceiptVerificationRateLimitPolicy";
    public const string AuthRateLimitPolicy = "AuthRateLimitPolicy";

    public static IServiceCollection AddAntiEnumerationRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(PolicyName, httpContext =>
            {
                var clientIp = ResolveClientIp(httpContext);
                var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                var partitionKey = $"{clientIp}_{userId}";

                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
                    SegmentsPerWindow = 3,
                    QueueLimit = 0
                });
            });

            options.AddPolicy(PublicReceiptVerificationPolicy, httpContext =>
            {
                var clientIp = ResolveClientIp(httpContext);

                return RateLimitPartition.GetSlidingWindowLimiter(clientIp, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 2,
                    QueueLimit = 0
                });
            });

            options.AddPolicy(AuthRateLimitPolicy, httpContext =>
            {
                var clientIp = ResolveClientIp(httpContext);

                return RateLimitPartition.GetSlidingWindowLimiter(clientIp, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 2,
                    QueueLimit = 0
                });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }
                else
                {
                    context.HttpContext.Response.Headers.RetryAfter = "60";
                }

                var localizer = context.HttpContext.RequestServices.GetService<IStringLocalizer<DomainMessages>>();
                var detail = localizer?["RATE_LIMIT_EXCEEDED"]?.Value
                    ?? "Rate limit exceeded. Please wait and try again later.";
                var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

                var problemDetails = new ProblemDetails
                {
                    Type = "https://tahdeera.com/errors/RATE_LIMIT_EXCEEDED",
                    Title = "Too Many Requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = detail,
                    Instance = context.HttpContext.Request.Path
                };
                problemDetails.Extensions["errorCode"] = "RATE_LIMIT_EXCEEDED";
                problemDetails.Extensions["traceId"] = traceId;

                await context.HttpContext.Response.WriteAsJsonAsync(
                    problemDetails,
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken: token);
            };
        });

        return services;
    }

    private static string ResolveClientIp(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var commaIndex = forwardedFor.IndexOf(',');
            return (commaIndex >= 0 ? forwardedFor[..commaIndex] : forwardedFor).Trim();
        }

        if (httpContext.Connection.RemoteIpAddress != null)
        {
            return httpContext.Connection.RemoteIpAddress.ToString();
        }

        return httpContext.Connection.Id ?? httpContext.TraceIdentifier;
    }
}
