using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Auth.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetCurrentUserQueryHandler(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.AppUsers.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            return Error.NotFound("USER_NOT_FOUND", "User not found.");
        }

        if (!user.IsActive)
        {
            return Error.Unauthorized("AUTH_USER_DEACTIVATED", "تم تعطيل حساب المستخدم. يرجى التواصل مع الإدارة.");
        }

        var memberships = await _unitOfWork.WorkspaceMembers.FindAsync(
            m => m.UserId == user.Id && m.IsActive,
            cancellationToken);

        var workspaceDtos = new List<UserWorkspaceMembershipDto>();
        foreach (var membership in memberships)
        {
            var workspace = await _unitOfWork.Workspaces.GetByIdAsync(membership.WorkspaceId, cancellationToken);
            if (workspace == null) continue;

            var subscriptions = await _unitOfWork.WorkspaceSubscriptions.FindAsync(
                s => s.WorkspaceId == workspace.Id,
                cancellationToken);

            var latestSub = subscriptions.OrderByDescending(s => s.EndsAt).FirstOrDefault();
            WorkspaceSubscriptionSummaryDto? subSummary = null;
            if (latestSub != null)
            {
                var isLockedOut = latestSub.ManualSuspendedAt != null || latestSub.EndsAt < _dateTimeProvider.UtcNow;
                subSummary = new WorkspaceSubscriptionSummaryDto(
                    latestSub.Status,
                    latestSub.StartsAt,
                    latestSub.EndsAt,
                    isLockedOut);
            }

            workspaceDtos.Add(new UserWorkspaceMembershipDto(
                workspace.Id,
                workspace.Name,
                workspace.Kind,
                membership.Role,
                subSummary));
        }

        var userDto = new UserProfileDto(
            user.Id,
            user.Username,
            user.NormalizedEmail,
            user.DisplayName,
            user.Phone,
            user.PreferredLanguage,
            user.PreferredTheme,
            user.IsPlatformAdmin);

        return Result.Success(new CurrentUserDto(userDto, workspaceDtos));
    }
}
