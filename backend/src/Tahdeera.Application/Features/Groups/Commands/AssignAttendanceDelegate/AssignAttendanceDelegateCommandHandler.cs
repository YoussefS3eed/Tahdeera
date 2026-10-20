using Microsoft.EntityFrameworkCore;
using Mapster;
using MediatR;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Groups.Commands.AssignAttendanceDelegate;

public sealed class AssignAttendanceDelegateCommandHandler : IRequestHandler<AssignAttendanceDelegateCommand, Result<GroupDelegationDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public AssignAttendanceDelegateCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GroupDelegationDto>> Handle(AssignAttendanceDelegateCommand request, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetByIdForWorkspaceAsync(request.WorkspaceId, request.GroupId, cancellationToken);

        if (group is null)
        {
            return DomainErrors.Groups.NotFound;
        }

        // 1. Revoke any existing active delegation to enforce single-active delegate constraint
        var now = DateTimeOffset.UtcNow;
        var activeDelegations = await _unitOfWork.GroupAttendanceDelegations
            .FindAsync(d => d.GroupId == request.GroupId && d.RevokedAt == null, cancellationToken);

        foreach (var active in activeDelegations)
        {
            active.Revoke(request.TeacherUserId, "Replaced by new delegate", now);
        }

        // 2. Create new delegation
        var newDelegation = GroupAttendanceDelegation.Create(
            request.GroupId,
            request.AssistantUserId,
            request.TeacherUserId);

        await _unitOfWork.GroupAttendanceDelegations.AddAsync(newDelegation, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return DomainErrors.Groups.ConcurrentDelegationConflict;
        }

        // 3. Resolve assistant user name
        var assistantUser = await _unitOfWork.AppUsers.GetByIdAsync(request.AssistantUserId, cancellationToken);

        var dto = newDelegation.Adapt<GroupDelegationDto>() with
        {
            AssistantName = assistantUser?.DisplayName ?? string.Empty
        };

        return Result.Success(dto);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        if (message.Contains("ux_one_active_attendance_delegate_per_group") || message.Contains("23505") || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var sqlStateProp = ex.InnerException?.GetType().GetProperty("SqlState");
        if (sqlStateProp?.GetValue(ex.InnerException) is string sqlState && sqlState == "23505")
        {
            return true;
        }

        return false;
    }
}