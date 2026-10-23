using MediatR;
using System.Net;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Finances.Commands.ReverseStudentPayment;

public class ReverseStudentPaymentHandler : IRequestHandler<ReverseStudentPaymentCommand, ReversePaymentResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ReverseStudentPaymentHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ReversePaymentResultDto> Handle(ReverseStudentPaymentCommand request, CancellationToken cancellationToken)
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
            throw new DomainRuleViolationException(
                "STUDENT_ACCOUNT_NOT_FOUND",
                "Student account record not found.",
                HttpStatusCode.NotFound);
        }

        var payment = await _unitOfWork.StudentPayments.GetByIdWithAllocationsAsync(request.PaymentId, cancellationToken);
        if (payment == null || payment.StudentAccountId != account.Id)
        {
            throw new DomainRuleViolationException(
                "PAYMENT_NOT_FOUND",
                "Payment not found for this student account.",
                HttpStatusCode.NotFound);
        }

        if (payment.Status == PaymentStatus.Reversed)
        {
            throw new DomainRuleViolationException(
                "PAYMENT_ALREADY_REVERSED",
                "This payment has already been reversed.",
                HttpStatusCode.BadRequest);
        }

        var activeAllocations = payment.Allocations.Where(a => !a.IsReversed).ToList();
        var restoredArrears = activeAllocations.Sum(a => a.Amount);
        var reopenedChargesCount = activeAllocations.Select(a => a.ChargeId).Distinct().Count();

        payment.Reverse(request.Reason, _currentUserService.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReversePaymentResultDto(
            payment.Id,
            payment.Status.ToString(),
            request.Reason,
            payment.ReversedAt!.Value,
            reopenedChargesCount,
            restoredArrears);
    }
}
