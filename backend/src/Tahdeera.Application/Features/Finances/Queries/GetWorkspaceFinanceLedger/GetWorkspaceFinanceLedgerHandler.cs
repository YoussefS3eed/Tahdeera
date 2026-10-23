using MediatR;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceLedger;

public class GetWorkspaceFinanceLedgerHandler : IRequestHandler<GetWorkspaceFinanceLedgerQuery, PagedResult<WorkspaceFinanceLedgerItemDto>>
{
    private readonly ITahdeeraDbContext _context;

    public GetWorkspaceFinanceLedgerHandler(ITahdeeraDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<WorkspaceFinanceLedgerItemDto>> Handle(
        GetWorkspaceFinanceLedgerQuery request,
        CancellationToken cancellationToken)
    {
        var wsList = await _context.WorkspaceStudents
            .AsNoTracking()
            .Include(ws => ws.Student)
            .Include(ws => ws.StudentAccount)
            .Where(ws => ws.WorkspaceId == request.WorkspaceId)
            .ToListAsync(cancellationToken);

        var accountToWsMap = wsList
            .Where(ws => ws.StudentAccount != null)
            .ToDictionary(ws => ws.StudentAccount!.Id, ws => ws);

        var accountIds = accountToWsMap.Keys.ToList();

        if (accountIds.Count == 0)
        {
            return new PagedResult<WorkspaceFinanceLedgerItemDto>(
                Items: Array.Empty<WorkspaceFinanceLedgerItemDto>(),
                TotalCount: 0,
                PageIndex: request.PageIndex,
                PageSize: request.PageSize);
        }

        var items = new List<WorkspaceFinanceLedgerItemDto>();
        var txType = request.TransactionType?.Trim().ToLowerInvariant() ?? "all";

        // Ingest Charges
        if (txType is "all" or "charges" or "charge")
        {
            var chargesQuery = _context.StudentCharges
                .AsNoTracking()
                .Include(c => c.CreatedByUser)
                .Where(c => accountIds.Contains(c.StudentAccountId));

            if (request.FromDate.HasValue)
            {
                chargesQuery = chargesQuery.Where(c => c.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                chargesQuery = chargesQuery.Where(c => c.CreatedAt <= request.ToDate.Value);
            }

            var charges = await chargesQuery.ToListAsync(cancellationToken);

            foreach (var c in charges)
            {
                accountToWsMap.TryGetValue(c.StudentAccountId, out var ws);
                var student = ws?.Student;

                var recordedBy = !string.IsNullOrWhiteSpace(c.CreatedByUser?.DisplayName)
                    ? c.CreatedByUser.DisplayName
                    : (!string.IsNullOrWhiteSpace(c.CreatedByUser?.Username) ? c.CreatedByUser.Username : "System");

                items.Add(new WorkspaceFinanceLedgerItemDto(
                    Id: c.Id.ToString(),
                    StudentId: student?.Id ?? Guid.Empty,
                    StudentName: student?.FullName ?? string.Empty,
                    StudentCode: student?.StudentCode ?? string.Empty,
                    Amount: c.Amount,
                    Type: "Charge",
                    PaymentMethod: null,
                    RecordedAtUtc: c.CreatedAt,
                    RecordedByUserName: recordedBy,
                    Status: c.Status.ToString(),
                    ReversalReason: null,
                    CancellationReason: c.CancellationReason,
                    Notes: c.Notes));
            }
        }

        // Ingest Payments & Reversals
        if (txType is "all" or "payments" or "payment" or "reversals" or "reversal")
        {
            var paymentsQuery = _context.StudentPayments
                .AsNoTracking()
                .Include(p => p.RecordedByUser)
                .Where(p => accountIds.Contains(p.StudentAccountId));

            if (request.FromDate.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.PaidAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.PaidAt <= request.ToDate.Value);
            }

            var payments = await paymentsQuery.ToListAsync(cancellationToken);

            foreach (var p in payments)
            {
                accountToWsMap.TryGetValue(p.StudentAccountId, out var ws);
                var student = ws?.Student;

                var recordedBy = !string.IsNullOrWhiteSpace(p.RecordedByUser?.DisplayName)
                    ? p.RecordedByUser.DisplayName
                    : (!string.IsNullOrWhiteSpace(p.RecordedByUser?.Username) ? p.RecordedByUser.Username : "Cashier");

                var itemType = p.Status == PaymentStatus.Reversed ? "Reversal" : "Payment";

                items.Add(new WorkspaceFinanceLedgerItemDto(
                    Id: string.IsNullOrWhiteSpace(p.ReceiptNumber) ? p.Id.ToString() : p.ReceiptNumber,
                    StudentId: student?.Id ?? Guid.Empty,
                    StudentName: student?.FullName ?? string.Empty,
                    StudentCode: student?.StudentCode ?? string.Empty,
                    Amount: p.Amount,
                    Type: itemType,
                    PaymentMethod: p.Method.ToString(),
                    RecordedAtUtc: p.PaidAt,
                    RecordedByUserName: recordedBy,
                    Status: p.Status.ToString(),
                    ReversalReason: p.ReversalReason,
                    CancellationReason: null,
                    Notes: p.Notes));
            }
        }

        // Filter by Transaction Type (if payment or reversal explicitly specified)
        IEnumerable<WorkspaceFinanceLedgerItemDto> filtered = items;
        if (txType is "payment" or "payments")
        {
            filtered = filtered.Where(i => i.Type == "Payment");
        }
        else if (txType is "reversal" or "reversals")
        {
            filtered = filtered.Where(i => i.Type == "Reversal");
        }
        else if (txType is "charge" or "charges")
        {
            filtered = filtered.Where(i => i.Type == "Charge");
        }

        // Filter by Status if specified
        if (!string.IsNullOrWhiteSpace(request.Status) && !request.Status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(i => i.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = filtered
            .OrderByDescending(i => i.RecordedAtUtc)
            .ThenByDescending(i => i.Id)
            .ToList();

        var totalCount = ordered.Count;
        var pagedItems = ordered
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResult<WorkspaceFinanceLedgerItemDto>(
            Items: pagedItems,
            TotalCount: totalCount,
            PageIndex: request.PageIndex,
            PageSize: request.PageSize);
    }
}
