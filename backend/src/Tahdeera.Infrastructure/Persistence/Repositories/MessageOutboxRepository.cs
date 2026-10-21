using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class MessageOutboxRepository : GenericRepository<MessageOutbox>, IMessageOutboxRepository
{
    public MessageOutboxRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<MessageOutbox>> ClaimBatchWithSkipLockedAsync(int batchSize, CancellationToken ct = default)
    {
        var sql = @"
            WITH batch AS (
                SELECT id FROM message_outbox 
                WHERE status IN ('Pending', 'FailedTransient') 
                  AND (next_attempt_at IS NULL OR next_attempt_at <= now()) 
                ORDER BY created_at ASC 
                LIMIT {0} 
                FOR UPDATE SKIP LOCKED
            )
            UPDATE message_outbox 
            SET status = 'Processing', attempts = attempts + 1, last_attempt_at = now() 
            FROM batch 
            WHERE message_outbox.id = batch.id 
            RETURNING message_outbox.*;";

        return await _context.MessageOutboxes
            .FromSqlRaw(sql, batchSize)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MessageOutbox>> GetStaleLeasesAsync(TimeSpan leaseDuration, int batchSize, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(leaseDuration);
        return await _dbSet
            .Where(m => m.Status == OutboxMessageStatus.Processing && m.LastAttemptAt < cutoff)
            .OrderBy(m => m.LastAttemptAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task<MessageOutbox?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<MessageOutbox?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
    }

    public new async Task AddAsync(MessageOutbox message, CancellationToken ct = default)
    {
        await base.AddAsync(message, ct);
    }

    public async Task CancelPendingForSessionAsync(Guid sessionId, string reason, CancellationToken ct = default)
    {
        var messages = await _dbSet
            .Where(m => m.SessionId == sessionId && (m.Status == OutboxMessageStatus.Pending || m.Status == OutboxMessageStatus.PublishedToBroker))
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            message.Cancel(reason);
        }
    }
}
