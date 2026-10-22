using System.Globalization;
using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Application.Features.Finances.Queries.ExportWorkspaceFinanceCsv;

public class ExportWorkspaceFinanceCsvHandler : IRequestHandler<ExportWorkspaceFinanceCsvQuery, IAsyncEnumerable<string>>
{
    private readonly ITahdeeraDbContext _context;

    public ExportWorkspaceFinanceCsvHandler(ITahdeeraDbContext context)
    {
        _context = context;
    }

    public async Task<IAsyncEnumerable<string>> Handle(ExportWorkspaceFinanceCsvQuery request, CancellationToken cancellationToken)
    {
        // 1. Subscription Feature Flag Check: excelExport
        var subscription = await _context.WorkspaceSubscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Where(s => s.WorkspaceId == request.WorkspaceId && s.Status == "Active")
            .OrderByDescending(s => s.EndsAt)
            .FirstOrDefaultAsync(cancellationToken);

        bool isExcelExportEnabled = false;

        if (subscription?.Plan != null)
        {
            isExcelExportEnabled = subscription.Plan.Features?.ExcelExport ?? false;
        }

        if (subscription != null && !string.IsNullOrWhiteSpace(subscription.FeatureOverrides) && subscription.FeatureOverrides != "{}")
        {
            try
            {
                using var doc = JsonDocument.Parse(subscription.FeatureOverrides);
                if (doc.RootElement.TryGetProperty("excelExport", out var prop))
                {
                    isExcelExportEnabled = prop.GetBoolean();
                }
                else if (doc.RootElement.TryGetProperty("ExcelExport", out var propPascal))
                {
                    isExcelExportEnabled = propPascal.GetBoolean();
                }
            }
            catch
            {
                // Fallback to plan features
            }
        }

        if (!isExcelExportEnabled)
        {
            throw new DomainRuleViolationException(
                "FEATURE_FLAG_DISABLED",
                "This feature is disabled in your workspace's current subscription plan.",
                HttpStatusCode.Forbidden);
        }

        // 2. Resolve Workspace Timezone
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, cancellationToken);

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(workspace?.TimezoneId ?? "Africa/Cairo");
        }
        catch
        {
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            }
            catch
            {
                tz = TimeZoneInfo.Utc;
            }
        }

        // 3. Resolve Workspace Student Accounts
        var workspaceStudents = await _context.WorkspaceStudents
            .AsNoTracking()
            .Include(ws => ws.Student)
            .Include(ws => ws.StudentAccount)
            .Where(ws => ws.WorkspaceId == request.WorkspaceId)
            .ToListAsync(cancellationToken);

        var accountMap = workspaceStudents
            .Where(ws => ws.StudentAccount != null)
            .ToDictionary(ws => ws.StudentAccount!.Id, ws => ws);

        var accountIds = accountMap.Keys.ToList();

        var items = new List<ExportItem>();
        var txType = request.TransactionType?.Trim().ToLowerInvariant() ?? "all";

        // 4. Ingest Charges
        if (txType is "all" or "charges")
        {
            var chargesQuery = _context.StudentCharges
                .AsNoTracking()
                .Include(c => c.Group)
                .Include(c => c.CreatedByUser)
                .Where(c => accountIds.Contains(c.StudentAccountId) &&
                            c.CreatedAt >= request.FromDate &&
                            c.CreatedAt <= request.ToDate);

            if (request.GroupId.HasValue)
            {
                chargesQuery = chargesQuery.Where(c => c.GroupId == request.GroupId.Value);
            }

            if (request.CashierUserId.HasValue)
            {
                chargesQuery = chargesQuery.Where(c => c.CreatedByUserId == request.CashierUserId.Value);
            }

            var charges = await chargesQuery.ToListAsync(cancellationToken);

            foreach (var c in charges)
            {
                accountMap.TryGetValue(c.StudentAccountId, out var ws);
                items.Add(new ExportItem(
                    DateUtc: c.CreatedAt,
                    ReceiptOrRef: "-",
                    Type: "Charge",
                    StudentCode: ws?.Student?.StudentCode ?? "-",
                    StudentName: ws?.Student?.FullName ?? "-",
                    SeatNumber: ws?.SeatNumber ?? "-",
                    GroupName: c.Group?.Name ?? "-",
                    BillingPeriod: c.BillingPeriodLabel ?? "-",
                    Description: c.Description,
                    Amount: c.Amount,
                    PaymentMethod: "-",
                    CashierOrUser: c.CreatedByUser?.DisplayName ?? "-",
                    Status: c.Status.ToString(),
                    ReversalReason: string.Empty));
            }
        }

        // 5. Ingest Payments & Reversals
        if (txType is "all" or "payments" or "reversals")
        {
            var paymentsQuery = _context.StudentPayments
                .AsNoTracking()
                .Include(p => p.Allocations)
                    .ThenInclude(a => a.Charge)
                        .ThenInclude(c => c!.Group)
                .Include(p => p.RecordedByUser)
                .Include(p => p.ReversedByUser)
                .Where(p => accountIds.Contains(p.StudentAccountId) &&
                            p.PaidAt >= request.FromDate &&
                            p.PaidAt <= request.ToDate);

            if (request.CashierUserId.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.RecordedByUserId == request.CashierUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                if (Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var methodEnum))
                {
                    paymentsQuery = paymentsQuery.Where(p => p.Method == methodEnum);
                }
            }

            if (request.GroupId.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.Allocations.Any(a => a.Charge != null && a.Charge.GroupId == request.GroupId.Value));
            }

            var payments = await paymentsQuery.ToListAsync(cancellationToken);

            foreach (var p in payments)
            {
                accountMap.TryGetValue(p.StudentAccountId, out var ws);
                var activeAlloc = p.Allocations.FirstOrDefault(a => !a.IsReversed);
                var groupName = activeAlloc?.Charge?.Group?.Name ?? "-";
                var billingPeriod = activeAlloc?.Charge?.BillingPeriodLabel ?? "-";

                if (txType is "all" or "payments")
                {
                    items.Add(new ExportItem(
                        DateUtc: p.PaidAt,
                        ReceiptOrRef: p.ReceiptNumber,
                        Type: "Payment",
                        StudentCode: ws?.Student?.StudentCode ?? "-",
                        StudentName: ws?.Student?.FullName ?? "-",
                        SeatNumber: ws?.SeatNumber ?? "-",
                        GroupName: groupName,
                        BillingPeriod: billingPeriod,
                        Description: p.Notes ?? "سداد رسوم",
                        Amount: p.Amount,
                        PaymentMethod: p.Method.ToString(),
                        CashierOrUser: p.RecordedByUser?.DisplayName ?? "-",
                        Status: p.Status.ToString(),
                        ReversalReason: p.ReversalReason ?? string.Empty));
                }

                if (p.Status == PaymentStatus.Reversed && p.ReversedAt.HasValue && txType is "all" or "reversals")
                {
                    items.Add(new ExportItem(
                        DateUtc: p.ReversedAt.Value,
                        ReceiptOrRef: p.ReceiptNumber,
                        Type: "Reversal",
                        StudentCode: ws?.Student?.StudentCode ?? "-",
                        StudentName: ws?.Student?.FullName ?? "-",
                        SeatNumber: ws?.SeatNumber ?? "-",
                        GroupName: groupName,
                        BillingPeriod: billingPeriod,
                        Description: $"إلغاء إيصال {p.ReceiptNumber}",
                        Amount: -p.Amount,
                        PaymentMethod: p.Method.ToString(),
                        CashierOrUser: p.ReversedByUser?.DisplayName ?? "-",
                        Status: "Reversed",
                        ReversalReason: p.ReversalReason ?? string.Empty));
                }
            }
        }

        // 6. Chronological Order by Date Ascending
        var sorted = items.OrderBy(i => i.DateUtc).ToList();

        return StreamCsvRowsAsync(sorted, tz);
    }

    private static async IAsyncEnumerable<string> StreamCsvRowsAsync(List<ExportItem> items, TimeZoneInfo tz)
    {
        yield return "Date (UTC),Date (Local),Receipt/Ref,Type,Student Code,Student Name,Seat Number,Group,Billing Period,Description,Amount (EGP),Payment Method,Cashier/User,Status,Reversal Reason\r\n";

        foreach (var item in items)
        {
            var dateLocal = TimeZoneInfo.ConvertTime(item.DateUtc, tz);
            yield return $"{Escape(item.DateUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}," +
                         $"{Escape(dateLocal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}," +
                         $"{Escape(item.ReceiptOrRef)}," +
                         $"{Escape(item.Type)}," +
                         $"{Escape(item.StudentCode)}," +
                         $"{Escape(item.StudentName)}," +
                         $"{Escape(item.SeatNumber)}," +
                         $"{Escape(item.GroupName)}," +
                         $"{Escape(item.BillingPeriod)}," +
                         $"{Escape(item.Description)}," +
                         $"{item.Amount.ToString("F2", CultureInfo.InvariantCulture)}," +
                         $"{Escape(item.PaymentMethod)}," +
                         $"{Escape(item.CashierOrUser)}," +
                         $"{Escape(item.Status)}," +
                         $"{Escape(item.ReversalReason)}\r\n";
        }

        await Task.CompletedTask;
    }

    private static string Escape(string? val)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        if (val.Contains(',') || val.Contains('"') || val.Contains('\n') || val.Contains('\r'))
        {
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }
        return val;
    }

    private record ExportItem(
        DateTimeOffset DateUtc,
        string ReceiptOrRef,
        string Type,
        string StudentCode,
        string StudentName,
        string SeatNumber,
        string GroupName,
        string BillingPeriod,
        string Description,
        decimal Amount,
        string PaymentMethod,
        string CashierOrUser,
        string Status,
        string ReversalReason
    );
}
