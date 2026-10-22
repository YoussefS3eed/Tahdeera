using System.Net;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Finances.Commands.BatchCreateGroupCharges;

public class BatchCreateGroupChargesHandler : IRequestHandler<BatchCreateGroupChargesCommand, BatchCreateGroupChargesResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BatchCreateGroupChargesHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<BatchCreateGroupChargesResultDto> Handle(BatchCreateGroupChargesCommand request, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetByIdAsync(request.GroupId, cancellationToken);
        if (group == null || group.OwnerWorkspaceId != request.WorkspaceId)
        {
            throw new DomainRuleViolationException(
                "GROUP_NOT_FOUND_IN_WORKSPACE",
                "The specified group was not found in this workspace.",
                HttpStatusCode.NotFound);
        }

        var activeGroupStudents = await _unitOfWork.GroupStudents.FindAsync(
            gs => gs.GroupId == request.GroupId && gs.Status == GroupStudentStatus.Active,
            cancellationToken);

        if (activeGroupStudents.Count == 0)
        {
            throw new DomainRuleViolationException(
                "NO_ACTIVE_STUDENTS_IN_GROUP",
                "No active students enrolled in this group to generate charges.",
                HttpStatusCode.BadRequest);
        }

        int chargesCreatedCount = 0;
        int skippedDuplicatesCount = 0;
        int autoSettledFromCreditCount = 0;
        decimal totalBilledAmount = 0m;
        var createdChargeIds = new List<Guid>();

        foreach (var gs in activeGroupStudents)
        {
            var ws = await _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(
                request.WorkspaceId, gs.StudentId, cancellationToken);

            if (ws == null)
            {
                continue;
            }

            var account = await _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdAsync(ws.Id, cancellationToken);
            if (account == null)
            {
                account = StudentAccount.Create(ws.Id, _currentUserService.UserId);
                await _unitOfWork.StudentAccounts.AddAsync(account, cancellationToken);
            }

            var openCharges = await _unitOfWork.StudentCharges.GetOpenChargesByAccountIdAsync(account.Id, cancellationToken);
            var isDuplicate = openCharges.Any(c =>
                c.GroupId == request.GroupId &&
                c.BillingPeriodLabel == request.BillingPeriodLabel &&
                c.Status == ChargeStatus.Open);

            if (isDuplicate)
            {
                skippedDuplicatesCount++;
                continue;
            }

            var charge = StudentCharge.Create(
                account.Id,
                request.Description,
                request.Amount,
                request.GroupId,
                request.BillingPeriodLabel,
                request.DueAt,
                request.Notes,
                _currentUserService.UserId);

            await _unitOfWork.StudentCharges.AddAsync(charge, cancellationToken);
            createdChargeIds.Add(charge.Id);
            chargesCreatedCount++;
            totalBilledAmount += charge.Amount;

            // FIFO Auto-Allocation from existing prepaid student credit
            var payments = await _unitOfWork.StudentPayments.GetPaymentsByAccountIdWithAllocationsAsync(account.Id, cancellationToken);
            decimal remainingChargeToSettle = charge.Amount;
            bool wasSettledFromCredit = false;

            foreach (var p in payments)
            {
                if (remainingChargeToSettle <= 0)
                {
                    break;
                }

                var activeAllocated = p.Allocations.Where(a => !a.IsReversed).Sum(a => a.Amount);
                var unallocated = p.Amount - activeAllocated;

                if (unallocated > 0)
                {
                    var allocAmount = Math.Min(unallocated, remainingChargeToSettle);
                    var allocation = PaymentAllocation.Create(p.Id, charge.Id, allocAmount);
                    p.AddAllocation(allocation);
                    await _unitOfWork.PaymentAllocations.AddAsync(allocation, cancellationToken);
                    remainingChargeToSettle -= allocAmount;
                    wasSettledFromCredit = true;
                }
            }

            if (wasSettledFromCredit)
            {
                autoSettledFromCreditCount++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BatchCreateGroupChargesResultDto(
            GroupId: group.Id,
            GroupName: group.Name,
            BillingPeriodLabel: request.BillingPeriodLabel,
            TotalActiveEnrolled: activeGroupStudents.Count,
            ChargesCreatedCount: chargesCreatedCount,
            SkippedDuplicatesCount: skippedDuplicatesCount,
            AutoSettledFromCreditCount: autoSettledFromCreditCount,
            TotalBilledAmount: totalBilledAmount,
            CreatedChargeIds: createdChargeIds);
    }
}
