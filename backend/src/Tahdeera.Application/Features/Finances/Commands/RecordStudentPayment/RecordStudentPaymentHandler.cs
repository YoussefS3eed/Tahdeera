using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Logging;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.Features.Finances.Services;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Finances.Commands.RecordStudentPayment;

public class RecordStudentPaymentHandler : IRequestHandler<RecordStudentPaymentCommand, RecordPaymentResultDto>
{
    private static readonly Regex EgyptianPhoneRegex = new(@"^(\+20|0)?1[0125][0-9]{8}$", RegexOptions.Compiled);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentAllocationEngine _allocationEngine;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RecordStudentPaymentHandler> _logger;

    public RecordStudentPaymentHandler(
        IUnitOfWork unitOfWork,
        IPaymentAllocationEngine allocationEngine,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ILogger<RecordStudentPaymentHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _allocationEngine = allocationEngine;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<RecordPaymentResultDto> Handle(RecordStudentPaymentCommand request, CancellationToken cancellationToken)
    {
        var ws = await _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(
            request.WorkspaceId, request.StudentId, cancellationToken);

        if (ws == null)
        {
            throw new DomainRuleViolationException(
                "STUDENT_NOT_IN_WORKSPACE",
                "The student is not enrolled in this workspace.",
                HttpStatusCode.NotFound);
        }

        var account = await _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdForUpdateAsync(ws.Id, cancellationToken);
        if (account == null)
        {
            account = StudentAccount.Create(ws.Id, _currentUserService.UserId);
            await _unitOfWork.StudentAccounts.AddAsync(account, cancellationToken);
        }

        var now = _dateTimeProvider.UtcNow;
        var yearMonth = int.Parse(now.ToString("yyyyMM"));
        var sequence = await _unitOfWork.WorkspaceReceiptSequences.GetNextSequenceAsync(request.WorkspaceId, yearMonth, cancellationToken);
        var receiptNumber = ReceiptNumberGenerator.Generate(yearMonth, request.WorkspaceId, sequence);

        var payment = StudentPayment.Create(
            account.Id,
            receiptNumber,
            request.Amount,
            request.Method,
            _currentUserService.UserId ?? Guid.Empty,
            request.PaidAt,
            request.Reference,
            request.Notes);

        var allocResult = await _allocationEngine.AllocatePaymentAsync(payment, account.Id, request.Allocations, cancellationToken);

        bool whatsAppReceiptStaged = false;
        if (ws.WhatsappOptIn && !string.IsNullOrWhiteSpace(ws.GuardianPhone))
        {
            var phone = ws.GuardianPhone.Trim();
            if (EgyptianPhoneRegex.IsMatch(phone))
            {
                var student = await _unitOfWork.Students.GetByIdAsync(ws.StudentId, cancellationToken);
                var studentName = student?.FullName ?? "الطالب";

                var payload = JsonSerializer.Serialize(new
                {
                    studentName,
                    receiptNumber = payment.ReceiptNumber,
                    amount = $"{payment.Amount:F2} EGP",
                    method = payment.Method.ToString(),
                    unallocatedCredit = $"{allocResult.UnallocatedCredit:F2} EGP"
                });

                var outbox = MessageOutbox.Create(
                    request.WorkspaceId,
                    phone,
                    "PaymentReceipt",
                    "payment_receipt_v1",
                    "ar",
                    payload,
                    $"pay_{payment.Id}",
                    studentId: ws.StudentId);

                await _unitOfWork.MessageOutbox.AddAsync(outbox, cancellationToken);
                whatsAppReceiptStaged = true;
            }
            else
            {
                _logger.LogWarning(
                    "WhatsApp receipt skipped for payment {PaymentId}: Guardian phone '{Phone}' is not in valid Egyptian format.",
                    payment.Id,
                    phone);
            }
        }

        await _unitOfWork.StudentPayments.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RecordPaymentResultDto(
            payment.Id,
            payment.ReceiptNumber,
            payment.Amount,
            payment.Method.ToString(),
            payment.PaidAt,
            allocResult.TotalAllocated,
            allocResult.UnallocatedCredit,
            payment.Status.ToString(),
            whatsAppReceiptStaged);
    }
}
