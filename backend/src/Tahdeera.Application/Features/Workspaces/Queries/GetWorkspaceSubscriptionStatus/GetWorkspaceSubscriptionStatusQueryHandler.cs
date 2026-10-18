using MediatR;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Workspaces.Queries.GetWorkspaceSubscriptionStatus;

public class GetWorkspaceSubscriptionStatusQueryHandler : IRequestHandler<GetWorkspaceSubscriptionStatusQuery, Result<WorkspaceSubscriptionStatusDto>>
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetWorkspaceSubscriptionStatusQueryHandler(
        ITahdeeraDbContext dbContext,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<WorkspaceSubscriptionStatusDto>> Handle(GetWorkspaceSubscriptionStatusQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Error.Unauthorized("AUTH_UNAUTHORIZED", "User is not authenticated.");
        }

        // Verify caller is a member of this workspace or Platform Admin
        if (!_currentUserService.IsPlatformAdmin)
        {
            var isMember = await _dbContext.WorkspaceMembers
                .AsNoTracking()
                .AnyAsync(m => m.WorkspaceId == request.WorkspaceId && m.UserId == currentUserId.Value && m.IsActive, cancellationToken);

            if (!isMember)
            {
                return Error.Forbidden("UNAUTHORIZED_WORKSPACE_ACCESS", "ليس لديك صلاحية الوصول إلى مساحة العمل هذه.");
            }
        }

        var subscription = await _dbContext.WorkspaceSubscriptions
            .AsNoTracking()
            .Where(s => s.WorkspaceId == request.WorkspaceId)
            .Include(s => s.Plan)
            .OrderByDescending(s => s.EndsAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription == null)
        {
            return Error.NotFound("WORKSPACE_NOT_FOUND", "مساحة العمل المطلوبة غير موجودة أو لا تمتلك اشتراكاً مسجلاً.");
        }

        var now = _dateTimeProvider.UtcNow;
        var isLockedOut = subscription.IsLockedOut(now);
        var isSuspended = subscription.ManualSuspendedAt != null;

        var maxStudents = subscription.MaxStudentsOverride ?? subscription.Plan?.MaxStudents ?? 0;
        var maxStaff = subscription.MaxStaffOverride ?? subscription.Plan?.MaxStaff ?? 0;
        object features = (object?)subscription.Plan?.Features ?? new { };

        var dto = new WorkspaceSubscriptionStatusDto(
            request.WorkspaceId,
            subscription.Plan?.NameAr,
            subscription.Plan?.NameEn,
            subscription.Status,
            subscription.StartsAt,
            subscription.EndsAt,
            isLockedOut,
            isSuspended,
            subscription.SuspensionReason,
            "01272785618",
            "01019482687",
            maxStudents,
            maxStaff,
            features);

        return Result<WorkspaceSubscriptionStatusDto>.Success(dto);
    }
}
