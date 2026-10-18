using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Tahdeera.Application.Common.Interfaces;

namespace Tahdeera.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId?.ToString() ?? "Anonymous";
        var workspaceId = _currentUserService.ActiveWorkspaceId?.ToString() ?? "None";
        var traceId = Activity.Current?.Id ?? "None";

        _logger.LogInformation(
            "Handling {RequestName} [TraceId: {TraceId}, UserId: {UserId}, WorkspaceId: {WorkspaceId}]",
            requestName, traceId, userId, workspaceId);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds}ms [TraceId: {TraceId}]",
                requestName, stopwatch.ElapsedMilliseconds, traceId);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Error handling {RequestName} after {ElapsedMilliseconds}ms [TraceId: {TraceId}]",
                requestName, stopwatch.ElapsedMilliseconds, traceId);
            throw;
        }
    }
}
