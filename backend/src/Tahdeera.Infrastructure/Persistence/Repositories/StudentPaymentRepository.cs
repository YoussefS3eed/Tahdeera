using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class StudentPaymentRepository : GenericRepository<StudentPayment>, IStudentPaymentRepository
{
    public StudentPaymentRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<StudentPayment?> GetByIdWithAllocationsAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<StudentPayment?> GetByReceiptNumberAsync(string receiptNumber, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.ReceiptNumber == receiptNumber, ct);
    }

    public new async Task AddAsync(StudentPayment payment, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(payment, ct);
    }
}
