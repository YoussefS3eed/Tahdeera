using MediatR;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceSummary;

public class GetWorkspaceFinanceSummaryHandler : IRequestHandler<GetWorkspaceFinanceSummaryQuery, WorkspaceFinanceSummaryDto>
{
    private readonly ITahdeeraDbContext _context;

    public GetWorkspaceFinanceSummaryHandler(ITahdeeraDbContext context)
    {
        _context = context;
    }

    public async Task<WorkspaceFinanceSummaryDto> Handle(GetWorkspaceFinanceSummaryQuery request, CancellationToken cancellationToken)
    {
        var wsIds = await _context.WorkspaceStudents
            .AsNoTracking()
            .Where(w => w.WorkspaceId == request.WorkspaceId)
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);

        var accounts = await _context.StudentAccounts
            .AsNoTracking()
            .Include(a => a.Charges)
            .Include(a => a.Payments)
            .Where(a => wsIds.Contains(a.WorkspaceStudentId))
            .ToListAsync(cancellationToken);

        var totalBilled = accounts
            .SelectMany(a => a.Charges)
            .Where(c => c.Status != ChargeStatus.Cancelled)
            .Sum(c => c.Amount);

        var totalCollected = accounts
            .SelectMany(a => a.Payments)
            .Where(p => p.Status == PaymentStatus.Received)
            .Sum(p => p.Amount);

        decimal totalOutstandingArrears = 0.00m;
        int activeDebtorCount = 0;

        foreach (var account in accounts)
        {
            var openCharges = account.Charges.Where(c => c.Status == ChargeStatus.Open).Sum(c => c.Amount);
            var receivedPayments = account.Payments.Where(p => p.Status == PaymentStatus.Received).Sum(p => p.Amount);
            var balance = openCharges - receivedPayments;

            if (balance > 0)
            {
                totalOutstandingArrears += balance;
                activeDebtorCount++;
            }
        }

        var collectedByMethod = new Dictionary<string, decimal>
        {
            ["Cash"] = 0.00m,
            ["Wallet"] = 0.00m,
            ["BankTransfer"] = 0.00m,
            ["Card"] = 0.00m,
            ["Other"] = 0.00m
        };

        foreach (var payment in accounts.SelectMany(a => a.Payments).Where(p => p.Status == PaymentStatus.Received))
        {
            var key = payment.Method.ToString();
            if (!collectedByMethod.ContainsKey(key))
            {
                collectedByMethod[key] = 0.00m;
            }
            collectedByMethod[key] += payment.Amount;
        }

        return new WorkspaceFinanceSummaryDto(
            request.WorkspaceId,
            totalBilled,
            totalCollected,
            totalOutstandingArrears,
            activeDebtorCount,
            collectedByMethod);
    }
}
