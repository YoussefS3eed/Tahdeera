using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Utilities;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Application.Features.Finances.Queries.VerifyPrintableReceipt;

public class VerifyPrintableReceiptHandler : IRequestHandler<VerifyPrintableReceiptQuery, VerifiedReceiptDto>
{
    private readonly ITahdeeraDbContext _context;
    private readonly ILogger<VerifyPrintableReceiptHandler> _logger;

    public VerifyPrintableReceiptHandler(ITahdeeraDbContext context, ILogger<VerifyPrintableReceiptHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<VerifiedReceiptDto> Handle(
        VerifyPrintableReceiptQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReceiptNumber) || string.IsNullOrWhiteSpace(request.Hash))
        {
            throw new DomainRuleViolationException(
                "INVALID_RECEIPT_VERIFICATION_TOKEN",
                "The receipt verification token is invalid or the transaction does not exist.",
                HttpStatusCode.NotFound);
        }

        byte[] providedBytes;
        try
        {
            providedBytes = Convert.FromHexString(request.Hash.Trim());
        }
        catch
        {
            throw new DomainRuleViolationException(
                "INVALID_RECEIPT_VERIFICATION_TOKEN",
                "The receipt verification token is invalid or the transaction does not exist.",
                HttpStatusCode.NotFound);
        }

        if (providedBytes.Length != 32)
        {
            throw new DomainRuleViolationException(
                "INVALID_RECEIPT_VERIFICATION_TOKEN",
                "The receipt verification token is invalid or the transaction does not exist.",
                HttpStatusCode.NotFound);
        }

        var payment = await _context.StudentPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ReceiptNumber == request.ReceiptNumber.Trim(), cancellationToken);

        if (payment == null)
        {
            throw new DomainRuleViolationException(
                "INVALID_RECEIPT_VERIFICATION_TOKEN",
                "The receipt verification token is invalid or the transaction does not exist.",
                HttpStatusCode.NotFound);
        }

        var account = payment.StudentAccount ?? await _context.StudentAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == payment.StudentAccountId, cancellationToken);

        var wsStudent = account?.WorkspaceStudent ?? (account != null
            ? await _context.WorkspaceStudents
                .AsNoTracking()
                .FirstOrDefaultAsync(ws => ws.Id == account.WorkspaceStudentId, cancellationToken)
            : null);

        var workspaceId = wsStudent?.WorkspaceId ?? Guid.Empty;

        var workspace = wsStudent?.Workspace ?? (workspaceId != Guid.Empty
            ? await _context.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken)
            : null);

        var student = wsStudent?.Student ?? (wsStudent != null
            ? await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == wsStudent.StudentId, cancellationToken)
            : null);

        var keyBytes = Encoding.UTF8.GetBytes(workspaceId.ToString());

        // Candidate 1: Standard printable receipt signature (Invariant Culture):
        // ReceiptNumber:Amount:PaidAtUtc:WorkspaceId:CashierId signed with WorkspaceId
        var payload1 = FormattableString.Invariant($"{payment.ReceiptNumber}:{payment.Amount:F2}:{payment.PaidAt:O}:{workspaceId}:{payment.RecordedByUserId}");
        var hash1 = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(payload1));

        // Candidate 2: Canonical spec signature (Invariant Culture):
        // ReceiptNumber:Amount:PaidAtUtc signed with WorkspaceId
        var payload2 = FormattableString.Invariant($"{payment.ReceiptNumber}:{payment.Amount:F2}:{payment.PaidAt:O}");
        var hash2 = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(payload2));

        // Candidate 3: Alternative ISO-8601 formatting (Invariant Culture):
        var payload3 = FormattableString.Invariant($"{payment.ReceiptNumber}:{payment.Amount:F2}:{payment.PaidAt.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}");
        var hash3 = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(payload3));

        // Candidate 4: Standard printable receipt signature (Local Culture Fallback):
        var payload4 = $"{payment.ReceiptNumber}:{payment.Amount:F2}:{payment.PaidAt:O}:{workspaceId}:{payment.RecordedByUserId}";
        var hash4 = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(payload4));

        bool isMatch = CryptographicOperations.FixedTimeEquals(hash1, providedBytes)
                    || CryptographicOperations.FixedTimeEquals(hash2, providedBytes)
                    || CryptographicOperations.FixedTimeEquals(hash3, providedBytes)
                    || CryptographicOperations.FixedTimeEquals(hash4, providedBytes);

        if (!isMatch)
        {
            throw new DomainRuleViolationException(
                "INVALID_RECEIPT_VERIFICATION_TOKEN",
                "The receipt verification token is invalid or the transaction does not exist.",
                HttpStatusCode.NotFound);
        }

        var studentNameMasked = StudentPrivacyMasker.MaskEgyptianName(student?.FullName ?? string.Empty);
        bool isValid = payment.Status == PaymentStatus.Received;

        return new VerifiedReceiptDto(
            IsValid: isValid,
            ReceiptNumber: payment.ReceiptNumber,
            WorkspaceName: workspace?.Name ?? string.Empty,
            StudentNameMasked: studentNameMasked,
            AmountEgp: payment.Amount,
            PaidAtUtc: payment.PaidAt,
            PaymentMethod: payment.Method.ToString(),
            Status: payment.Status.ToString(),
            ReversedAtUtc: payment.ReversedAt);
    }
}
