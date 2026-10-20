using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IAttendanceRecordRepository : IGenericRepository<AttendanceRecord>
{
    Task<IReadOnlyList<AttendanceRecord>> GetActiveRecordsForSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetRecordsForSessionWithDetailsAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
