using System.Net;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Finances.Commands.CreateStudentCharge;

public class CreateStudentChargeHandler : IRequestHandler<CreateStudentChargeCommand, StudentChargeDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateStudentChargeHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<StudentChargeDto> Handle(CreateStudentChargeCommand request, CancellationToken cancellationToken)
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

        var account = await _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdAsync(ws.Id, cancellationToken);
        if (account == null)
        {
            account = StudentAccount.Create(ws.Id, _currentUserService.UserId);
            await _unitOfWork.StudentAccounts.AddAsync(account, cancellationToken);
        }

        string? groupName = null;
        if (request.GroupId.HasValue)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(request.GroupId.Value);
            if (group == null || group.OwnerWorkspaceId != request.WorkspaceId)
            {
                throw new DomainRuleViolationException(
                    "GROUP_NOT_FOUND_IN_WORKSPACE",
                    "Group does not belong to this workspace.",
                    HttpStatusCode.BadRequest);
            }
            groupName = group.Name;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StudentChargeDto(
            charge.Id,
            charge.StudentAccountId,
            charge.GroupId,
            groupName,
            charge.BillingPeriodLabel,
            charge.Description,
            charge.Amount,
            0.00m,
            charge.Amount,
            charge.DueAt,
            charge.Status.ToString(),
            charge.Notes,
            charge.CreatedAt);
    }
}
