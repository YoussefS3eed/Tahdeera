using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IMessageOutboxRepository
{
    Task<IReadOnlyList<MessageOutbox>> ClaimBatchWithSkipLockedAsync(int batchSize, CancellationToken ct = default);
    Task<IReadOnlyList<MessageOutbox>> GetStaleLeasesAsync(TimeSpan leaseDuration, int batchSize, CancellationToken ct = default);
    Task<MessageOutbox?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MessageOutbox?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);
    Task<MessageOutbox?> GetByProviderMessageIdAsync(string providerMessageId, CancellationToken ct = default);
    Task AddAsync(MessageOutbox message, CancellationToken ct = default);
    Task CancelPendingForSessionAsync(Guid sessionId, string reason, CancellationToken ct = default);
}
