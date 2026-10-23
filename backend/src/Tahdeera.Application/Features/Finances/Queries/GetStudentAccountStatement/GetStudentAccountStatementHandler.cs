using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Application.Features.Finances.Queries.GetStudentAccountStatement;

public class GetStudentAccountStatementHandler : IRequestHandler<GetStudentAccountStatementQuery, StudentAccountStatementDto>
{
    private readonly ITahdeeraDbContext _context;

    public GetStudentAccountStatementHandler(ITahdeeraDbContext context)
    {
        _context = context;
    }

    public async Task<StudentAccountStatementDto> Handle(GetStudentAccountStatementQuery request, CancellationToken cancellationToken)
    {
        var ws = await _context.WorkspaceStudents
            .AsNoTracking()
            .Include(w => w.Student)
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
            .Include(a => a.Charges)
                .ThenInclude(c => c.Allocations)
            .Include(a => a.Payments)
                .ThenInclude(p => p.Allocations)
                    .ThenInclude(al => al.Charge)
            .FirstOrDefaultAsync(a => a.WorkspaceStudentId == ws.Id, cancellationToken);

        if (account == null)
        {
            bool hasPeriodFilter = request.FromDate.HasValue || request.ToDate.HasValue;
            return new StudentAccountStatementDto(
                AccountId: Guid.Empty,
                WorkspaceStudentId: ws.Id,
                StudentId: ws.StudentId,
                StudentName: ws.Student?.FullName ?? string.Empty,
                SeatNumber: ws.SeatNumber,
                Currency: "EGP",
                CurrentBalance: 0.00m,
                BalanceStatus: "Settled",
                TotalLifetimeCharges: 0.00m,
                TotalLifetimePayments: 0.00m,
                TotalOpenCharges: 0.00m,
                TotalUnallocatedCredit: 0.00m,
                PeriodChargesTotal: hasPeriodFilter ? 0.00m : null,
                PeriodPaymentsTotal: hasPeriodFilter ? 0.00m : null,
                PeriodNetChange: hasPeriodFilter ? 0.00m : null,
                Transactions: Array.Empty<AccountTransactionDto>());
        }

        var totalOpenCharges = account.Charges
            .Where(c => c.Status == ChargeStatus.Open)
            .Sum(c => c.Amount);

        var totalReceivedPayments = account.Payments
            .Where(p => p.Status == PaymentStatus.Received)
            .Sum(p => p.Amount);

        var currentBalance = totalOpenCharges - totalReceivedPayments;

        var balanceStatus = currentBalance switch
        {
            0.00m => "Settled",
            > 0.00m => "InArrears",
            < 0.00m => "Overpaid"
        };

        var totalLifetimeCharges = account.Charges
            .Where(c => c.Status != ChargeStatus.Cancelled)
            .Sum(c => c.Amount);

        var totalLifetimePayments = totalReceivedPayments;

        var totalAllocated = account.Payments
            .Where(p => p.Status == PaymentStatus.Received)
            .SelectMany(p => p.Allocations)
            .Where(a => !a.IsReversed)
            .Sum(a => a.Amount);

        var totalUnallocatedCredit = Math.Max(0.00m, totalReceivedPayments - totalAllocated);

        decimal? periodChargesTotal = null;
        decimal? periodPaymentsTotal = null;
        decimal? periodNetChange = null;

        if (request.FromDate.HasValue || request.ToDate.HasValue)
        {
            var from = request.FromDate ?? DateTimeOffset.MinValue;
            var to = request.ToDate ?? DateTimeOffset.MaxValue;

            periodChargesTotal = account.Charges
                .Where(c => c.Status == ChargeStatus.Open && c.CreatedAt >= from && c.CreatedAt <= to)
                .Sum(c => c.Amount);

            periodPaymentsTotal = account.Payments
                .Where(p => p.Status == PaymentStatus.Received && p.PaidAt >= from && p.PaidAt <= to)
                .Sum(p => p.Amount);

            periodNetChange = periodChargesTotal - periodPaymentsTotal;
        }

        var transactions = new List<AccountTransactionDto>();

        foreach (var charge in account.Charges)
        {
            if (request.FromDate.HasValue && charge.CreatedAt < request.FromDate.Value) continue;
            if (request.ToDate.HasValue && charge.CreatedAt > request.ToDate.Value) continue;

            var activeAllocated = charge.Allocations.Where(a => !a.IsReversed).Sum(a => a.Amount);
            var remaining = Math.Max(0.00m, charge.Amount - activeAllocated);

            var allocDtos = charge.Allocations
                .Where(a => !a.IsReversed)
                .Select(a => new AllocationItemDto(charge.Id, charge.Description, a.Amount))
                .ToList();

            transactions.Add(new AccountTransactionDto(
                Id: charge.Id,
                Type: "Charge",
                Date: charge.CreatedAt,
                ReferenceOrReceipt: null,
                Description: charge.Description,
                Amount: charge.Amount,
                Status: charge.Status.ToString(),
                SettledAmount: activeAllocated,
                RemainingAmount: remaining,
                PaymentMethod: null,
                Allocations: allocDtos));
        }

        foreach (var payment in account.Payments)
        {
            if (request.FromDate.HasValue && payment.PaidAt < request.FromDate.Value) continue;
            if (request.ToDate.HasValue && payment.PaidAt > request.ToDate.Value) continue;

            var activeAllocations = payment.Allocations.Where(a => !a.IsReversed).ToList();
            var allocatedAmount = activeAllocations.Sum(a => a.Amount);
            var unallocatedAmount = Math.Max(0.00m, payment.Amount - allocatedAmount);

            var allocDtos = activeAllocations
                .Select(a => new AllocationItemDto(a.ChargeId, a.Charge?.Description ?? string.Empty, a.Amount))
                .ToList();

            transactions.Add(new AccountTransactionDto(
                Id: payment.Id,
                Type: "Payment",
                Date: payment.PaidAt,
                ReferenceOrReceipt: payment.ReceiptNumber,
                Description: $"Payment received via {payment.Method}",
                Amount: payment.Amount,
                Status: payment.Status.ToString(),
                SettledAmount: allocatedAmount,
                RemainingAmount: unallocatedAmount,
                PaymentMethod: payment.Method.ToString(),
                Allocations: allocDtos));
        }

        var orderedTransactions = transactions.OrderByDescending(t => t.Date).ToList();

        return new StudentAccountStatementDto(
            AccountId: account.Id,
            WorkspaceStudentId: ws.Id,
            StudentId: ws.StudentId,
            StudentName: ws.Student?.FullName ?? string.Empty,
            SeatNumber: ws.SeatNumber,
            Currency: account.Currency,
            CurrentBalance: currentBalance,
            BalanceStatus: balanceStatus,
            TotalLifetimeCharges: totalLifetimeCharges,
            TotalLifetimePayments: totalLifetimePayments,
            TotalOpenCharges: totalOpenCharges,
            TotalUnallocatedCredit: totalUnallocatedCredit,
            PeriodChargesTotal: periodChargesTotal,
            PeriodPaymentsTotal: periodPaymentsTotal,
            PeriodNetChange: periodNetChange,
            Transactions: orderedTransactions);
    }
}
