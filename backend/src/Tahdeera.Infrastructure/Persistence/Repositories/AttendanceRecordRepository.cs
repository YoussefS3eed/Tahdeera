using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class AttendanceRecordRepository : GenericRepository<AttendanceRecord>, IAttendanceRecordRepository
{
    public AttendanceRecordRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetActiveRecordsForSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.SessionId == sessionId && r.VoidedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetRecordsForSessionWithDetailsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Student)
            .Where(r => r.SessionId == sessionId)
            .ToListAsync(cancellationToken);
    }
}
