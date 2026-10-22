using System.Net;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Utilities;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Application.Features.Finances.Queries.GetPrintableReceipt;

public class GetPrintableReceiptHandler : IRequestHandler<GetPrintableReceiptQuery, PrintableReceiptDto>
{
    private readonly ITahdeeraDbContext _context;

    public GetPrintableReceiptHandler(ITahdeeraDbContext context)
    {
        _context = context;
    }

    public async Task<PrintableReceiptDto> Handle(GetPrintableReceiptQuery request, CancellationToken cancellationToken)
    {
        var ws = await _context.WorkspaceStudents
            .AsNoTracking()
            .Include(w => w.Workspace)
            .Include(w => w.Student)
                .ThenInclude(s => s.AcademicHistories)
                    .ThenInclude(h => h.Grade)
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
            throw new DomainRuleViolationException(
                "PAYMENT_NOT_FOUND",
                "Requested payment was not found in this account.",
                HttpStatusCode.NotFound);
        }

        var payment = await _context.StudentPayments
            .AsNoTracking()
            .Include(p => p.Allocations)
                .ThenInclude(a => a.Charge)
            .Include(p => p.RecordedByUser)
            .FirstOrDefaultAsync(
                p => p.Id == request.PaymentId && p.StudentAccountId == account.Id,
                cancellationToken);

        if (payment == null)
        {
            throw new DomainRuleViolationException(
                "PAYMENT_NOT_FOUND",
                "Requested payment was not found in this account.",
                HttpStatusCode.NotFound);
        }

        if (payment.Status == PaymentStatus.Reversed)
        {
            throw new DomainRuleViolationException(
                "CANNOT_PRINT_REVERSED_PAYMENT",
                "Cannot print receipt for a reversed payment.",
                HttpStatusCode.BadRequest);
        }

        var openCharges = await _context.StudentCharges
            .AsNoTracking()
            .Where(c => c.StudentAccountId == account.Id && c.Status == ChargeStatus.Open)
            .ToListAsync(cancellationToken);

        var receivedPayments = await _context.StudentPayments
            .AsNoTracking()
            .Where(p => p.StudentAccountId == account.Id && p.Status == PaymentStatus.Received)
            .ToListAsync(cancellationToken);

        decimal totalCharges = openCharges.Sum(c => c.Amount);
        decimal totalPayments = receivedPayments.Sum(p => p.Amount);
        decimal netDebt = totalCharges - totalPayments;

        string balanceStatus;
        decimal remainingAccountBalance;

        if (netDebt > 0)
        {
            balanceStatus = "InArrears";
            remainingAccountBalance = netDebt;
        }
        else if (netDebt < 0)
        {
            balanceStatus = "Overpaid";
            remainingAccountBalance = Math.Abs(netDebt);
        }
        else
        {
            balanceStatus = "Settled";
            remainingAccountBalance = 0.00m;
        }

        var settlements = payment.Allocations
            .Where(a => !a.IsReversed)
            .Select(a =>
            {
                var chargeAllocated = a.Charge?.Allocations.Where(ca => !ca.IsReversed).Sum(ca => ca.Amount) ?? a.Amount;
                var remainingCharge = Math.Max(0.00m, (a.Charge?.Amount ?? a.Amount) - chargeAllocated);

                return new ReceiptSettlementItemDto(
                    ChargeId: a.ChargeId,
                    Description: a.Charge?.Description ?? "رسوم دراسية",
                    BillingPeriodLabel: a.Charge?.BillingPeriodLabel,
                    AllocatedAmount: a.Amount,
                    RemainingChargeBalance: remainingCharge);
            }).ToList();

        string amountInWordsArabic = Tafqeet.Convert(payment.Amount, "EGP");

        var payloadToSign = $"{payment.ReceiptNumber}:{payment.Amount:F2}:{payment.PaidAt:O}:{request.WorkspaceId}:{payment.RecordedByUserId}";
        var keyBytes = Encoding.UTF8.GetBytes(request.WorkspaceId.ToString());
        var hashBytes = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(payloadToSign));
        var verificationHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var verificationPath = $"/verify-receipt/{verificationHash}";

        var gradeName = ws.Student.AcademicHistories
            .OrderByDescending(h => h.RecordedAt)
            .FirstOrDefault()?.Grade?.NameAr;

        var cashierName = payment.RecordedByUser?.DisplayName ?? "أمين الخزينة";

        return new PrintableReceiptDto(
            WorkspaceId: request.WorkspaceId,
            WorkspaceName: ws.Workspace?.Name ?? string.Empty,
            WorkspacePhone: ws.Workspace?.ContactPhone,
            WorkspaceAddress: ws.Workspace?.CityAddress,
            StudentId: request.StudentId,
            StudentCode: ws.Student?.StudentCode ?? string.Empty,
            StudentName: ws.Student?.FullName ?? string.Empty,
            SeatNumber: ws.SeatNumber,
            GradeName: gradeName,
            PaymentId: payment.Id,
            ReceiptNumber: payment.ReceiptNumber,
            PaidAt: payment.PaidAt,
            PaymentMethod: payment.Method.ToString(),
            Reference: payment.Reference,
            Amount: payment.Amount,
            Currency: "EGP",
            AmountInWordsArabic: amountInWordsArabic,
            CashierName: cashierName,
            Settlements: settlements,
            RemainingAccountBalance: remainingAccountBalance,
            BalanceStatus: balanceStatus,
            VerificationHash: verificationHash,
            VerificationPath: verificationPath);
    }
}
