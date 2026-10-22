using System.Net;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Finances.Commands.WriteOffStudentCharge;

public class WriteOffStudentChargeHandler : IRequestHandler<WriteOffStudentChargeCommand, WriteOffChargeResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public WriteOffStudentChargeHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<WriteOffChargeResultDto> Handle(WriteOffStudentChargeCommand request, CancellationToken cancellationToken)
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

        var charge = await _unitOfWork.StudentCharges.GetByIdAsync(request.ChargeId, cancellationToken);
        if (charge == null || charge.StudentAccountId != account.Id)
        {
            throw new DomainRuleViolationException(
                "CHARGE_NOT_FOUND",
                "Charge not found for this student account.",
                HttpStatusCode.NotFound);
        }

        charge.WriteOff(request.Reason, _currentUserService.UserId);

        var activeAllocations = await _unitOfWork.PaymentAllocations.GetActiveByChargeIdAsync(charge.Id, cancellationToken);
        decimal freedCredit = 0.00m;
        foreach (var allocation in activeAllocations)
        {
            allocation.Reverse();
            freedCredit += allocation.Amount;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new WriteOffChargeResultDto(
            charge.Id,
            charge.Status.ToString(),
            request.Reason,
            charge.CancelledAt!.Value,
            activeAllocations.Count,
            freedCredit);
    }
}
