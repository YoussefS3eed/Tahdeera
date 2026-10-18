using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Workspaces.Commands.ReactivateWorkspace;

public class ReactivateWorkspaceCommandHandler : IRequestHandler<ReactivateWorkspaceCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReactivateWorkspaceCommandHandler(
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ReactivateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Error.Unauthorized("AUTH_UNAUTHORIZED", "User is not authenticated.");
        }

        if (!_currentUserService.IsPlatformAdmin)
        {
            return Error.Forbidden("ROLE_FORBIDDEN", "لا تملك الصلاحية المطلوبة لتنفيذ هذا الإجراء.");
        }

        var subscriptions = await _unitOfWork.WorkspaceSubscriptions
            .FindAsync(s => s.WorkspaceId == request.WorkspaceId, cancellationToken);
        var latestSub = subscriptions.OrderByDescending(s => s.EndsAt).FirstOrDefault();

        if (latestSub == null)
        {
            return Error.NotFound("WORKSPACE_NOT_FOUND", "مساحة العمل المطلوبة غير موجودة.");
        }

        var now = _dateTimeProvider.UtcNow;
        latestSub.Reactivate(now);
        _unitOfWork.WorkspaceSubscriptions.Update(latestSub);

        await _auditLogService.LogAsync(
            actorUserId: currentUserId.Value,
            workspaceId: request.WorkspaceId,
            action: "WORKSPACE_REACTIVATED",
            entityType: "WorkspaceSubscription",
            entityId: latestSub.Id.ToString(),
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
