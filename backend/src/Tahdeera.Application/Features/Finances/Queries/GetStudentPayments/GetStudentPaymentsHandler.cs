using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Application.Features.Finances.Queries.GetStudentPayments;

public class GetStudentPaymentsHandler : IRequestHandler<GetStudentPaymentsQuery, List<StudentPaymentDto>>
{
    private readonly ITahdeeraDbContext _context;

    public GetStudentPaymentsHandler(ITahdeeraDbContext context)
    {
        _context = context;
    }

    public async Task<List<StudentPaymentDto>> Handle(GetStudentPaymentsQuery request, CancellationToken cancellationToken)
    {
        var ws = await _context.WorkspaceStudents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                w => w.WorkspaceId == request.WorkspaceId && w.StudentId == request.StudentId,
                cancellationToken);

        if (ws == null)
        {
            throw new DomainRuleViolationException(
                "STUDENT_NOT_IN_WORKSPACE",
                "The student is not enrolled in this workspace.",
                HttpStatusCode.NotFound);
        }

        var account = await _context.StudentAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.WorkspaceStudentId == ws.Id, cancellationToken);

        if (account == null)
        {
            return new List<StudentPaymentDto>();
        }

        var payments = await _context.StudentPayments
            .AsNoTracking()
            .Include(p => p.Allocations)
                .ThenInclude(a => a.Charge)
            .Where(p => p.StudentAccountId == account.Id)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync(cancellationToken);

        return payments.Select(p =>
        {
            var activeAllocations = p.Allocations.Where(a => !a.IsReversed).ToList();
            var allocatedAmount = activeAllocations.Sum(a => a.Amount);
            var unallocatedAmount = Math.Max(0.00m, p.Amount - allocatedAmount);

            var allocationDtos = p.Allocations.Select(a => new PaymentAllocationDetailDto(
                ChargeId: a.ChargeId,
                ChargeDescription: a.Charge?.Description ?? string.Empty,
                Amount: a.Amount,
                IsReversed: a.IsReversed
            )).ToList();

            return new StudentPaymentDto(
                Id: p.Id,
                StudentAccountId: p.StudentAccountId,
                ReceiptNumber: p.ReceiptNumber,
                Amount: p.Amount,
                PaidAt: p.PaidAt,
                Method: p.Method.ToString(),
                Reference: p.Reference,
                Notes: p.Notes,
                Status: p.Status.ToString(),
                ReversedAt: p.ReversedAt,
                ReversalReason: p.ReversalReason,
                AllocatedAmount: allocatedAmount,
                UnallocatedAmount: unallocatedAmount,
                Allocations: allocationDtos);
        }).ToList();
    }
}
