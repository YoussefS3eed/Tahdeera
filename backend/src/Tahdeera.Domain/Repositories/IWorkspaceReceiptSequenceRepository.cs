namespace Tahdeera.Domain.Repositories;

public interface IWorkspaceReceiptSequenceRepository
{
    Task<int> GetNextSequenceAsync(Guid workspaceId, int yearMonth, CancellationToken ct = default);
}
