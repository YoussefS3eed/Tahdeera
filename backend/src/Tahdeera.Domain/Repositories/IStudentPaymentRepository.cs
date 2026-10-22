using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IStudentPaymentRepository
{
    Task<StudentPayment?> GetByIdWithAllocationsAsync(Guid id, CancellationToken ct = default);
    Task<StudentPayment?> GetByReceiptNumberAsync(string receiptNumber, CancellationToken ct = default);
    Task<List<StudentPayment>> GetPaymentsByAccountIdWithAllocationsAsync(Guid accountId, CancellationToken ct = default);
    Task AddAsync(StudentPayment payment, CancellationToken ct = default);
}
