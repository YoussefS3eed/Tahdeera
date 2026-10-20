using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;

namespace Tahdeera.Infrastructure.BackgroundJobs;

public sealed class MessageOutboxRelayWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxWorkerOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<MessageOutboxRelayWorker> _logger;

    public MessageOutboxRelayWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxWorkerOptions> options,
        IDateTimeProvider dateTimeProvider,
        ILogger<MessageOutboxRelayWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageOutboxRelayWorker started with polling interval {Interval}ms", _options.PollingIntervalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RecoverStaleLeasesAsync(stoppingToken);
                await ProcessOutboxBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred during MessageOutboxRelayWorker execution cycle");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(_options.PollingIntervalMilliseconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("MessageOutboxRelayWorker stopped");
    }

    public async Task<int> RecoverStaleLeasesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var now = _dateTimeProvider.UtcNow;
        var staleThreshold = now.Subtract(TimeSpan.FromMinutes(_options.LeaseTimeoutMinutes));

        var staleMessages = await context.MessageOutboxes
            .Where(m => m.Status == OutboxMessageStatus.Processing && m.LastAttemptAt != null && m.LastAttemptAt < staleThreshold)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (staleMessages.Count == 0)
        {
            return 0;
        }

        _logger.LogWarning("Recovering {Count} stale outbox message leases past timeout ({Threshold})", staleMessages.Count, staleThreshold);

        foreach (var message in staleMessages)
        {
            message.ReleaseStaleLease(now, $"Lease expired after {_options.LeaseTimeoutMinutes} minutes without completion");
        }

        await context.SaveChangesAsync(cancellationToken);
        return staleMessages.Count;
    }

    public async Task<int> ProcessOutboxBatchAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessageOutboxPublisher>();
        var now = _dateTimeProvider.UtcNow;

        List<MessageOutbox> messages;

        // Use PostgreSQL FOR UPDATE SKIP LOCKED when running on Npgsql relational database
        if (context.Database.IsNpgsql())
        {
            var sql = """
                SELECT id FROM message_outbox
                WHERE status IN ('Pending', 'FailedTransient')
                  AND (next_attempt_at IS NULL OR next_attempt_at <= {0})
                ORDER BY created_at ASC
                LIMIT {1}
                FOR UPDATE SKIP LOCKED;
                """;

            var messageIds = await context.Database
                .SqlQueryRaw<Guid>(sql, now, _options.BatchSize)
                .ToListAsync(cancellationToken);

            if (messageIds.Count == 0)
            {
                return 0;
            }

            messages = await context.MessageOutboxes
                .Where(m => messageIds.Contains(m.Id))
                .ToListAsync(cancellationToken);
        }
        else
        {
            // In-Memory / SQLite fallback for unit & test harness execution
            messages = await context.MessageOutboxes
                .Where(m => (m.Status == OutboxMessageStatus.Pending || m.Status == OutboxMessageStatus.FailedTransient)
                         && (m.NextAttemptAt == null || m.NextAttemptAt <= now))
                .OrderBy(m => m.CreatedAt)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);
        }

        if (messages.Count == 0)
        {
            return 0;
        }

        // 1. Claim each message for processing
        foreach (var message in messages)
        {
            message.ClaimForProcessing(now);
        }

        // Commit claims
        await context.SaveChangesAsync(cancellationToken);

        // 2. Dispatch each message via publisher
        foreach (var message in messages)
        {
            try
            {
                var publishResult = await publisher.PublishAsync(message, cancellationToken);
                if (publishResult.Success)
                {
                    message.MarkPublishedToBroker(publishResult.BrokerMessageId ?? Guid.NewGuid().ToString("N"), now);
                }
                else
                {
                    message.ScheduleRetry(now, publishResult.ErrorCode ?? "BROKER_ERROR", publishResult.ErrorDetails ?? "Failed to publish");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error publishing outbox message {MessageId}", message.Id);
                message.ScheduleRetry(now, "PUBLISH_EXCEPTION", ex.Message);
            }
        }

        // Commit outcomes
        await context.SaveChangesAsync(cancellationToken);
        return messages.Count;
    }
}
