using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class StudentCredentialRepository : GenericRepository<StudentCredential>, IStudentCredentialRepository
{
    public StudentCredentialRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<StudentCredential?> GetActiveByTokenHashAsync(byte[] tokenHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.TokenHash == tokenHash && c.RevokedAt == null, cancellationToken);
    }

    public async Task<bool> TokenHashExistsAsync(byte[] tokenHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(c => c.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<StudentCredential?> GetActiveCardForStudentAsync(
        Guid workspaceId,
        Guid studentId,
        CredentialType type,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c =>
            c.WorkspaceId == workspaceId &&
            c.StudentId == studentId &&
            c.CredentialType == type &&
            c.RevokedAt == null, cancellationToken);
    }

    public async Task<IReadOnlyList<StudentCredential>> GetCredentialsByStudentAsync(
        Guid workspaceId,
        Guid studentId,
        bool includeUserDetails = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (includeUserDetails)
        {
            query = query
                .Include(c => c.IssuedByUser)
                .Include(c => c.RevokedByUser);
        }

        return await query
            .Where(c => c.WorkspaceId == workspaceId && c.StudentId == studentId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveCredentialAsync(
        Guid workspaceId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(c =>
            c.WorkspaceId == workspaceId &&
            c.StudentId == studentId &&
            c.RevokedAt == null, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetStudentIdsWithActiveCardsAsync(
        Guid workspaceId,
        IEnumerable<Guid> studentIds,
        CancellationToken cancellationToken = default)
    {
        var idList = studentIds.ToList();
        var result = await _dbSet.AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId &&
                        idList.Contains(c.StudentId) &&
                        c.RevokedAt == null)
            .Select(c => c.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return result.ToHashSet();
    }
}
