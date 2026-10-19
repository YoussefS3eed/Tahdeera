using Mapster;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Workspaces.Commands.DeactivateWorkspaceMember;

public class DeactivateWorkspaceMemberCommandHandler : IRequestHandler<DeactivateWorkspaceMemberCommand, Result<DeactivateWorkspaceMemberResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeactivateWorkspaceMemberCommandHandler(
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

    public async Task<Result<DeactivateWorkspaceMemberResultDto>> Handle(DeactivateWorkspaceMemberCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Error.Unauthorized("AUTH_UNAUTHORIZED", "User is not authenticated.");
        }

        // 1. Authorization: Only Owner or Platform Admin can deactivate members
        if (!_currentUserService.IsPlatformAdmin)
        {
            var isOwner = await _unitOfWork.WorkspaceMembers.AnyAsync(
                m => m.WorkspaceId == request.WorkspaceId && m.UserId == currentUserId.Value && m.Role == "Owner" && m.IsActive,
                cancellationToken);

            if (!isOwner)
            {
                return Error.Forbidden("ROLE_FORBIDDEN", "لا تملك الصلاحية المطلوبة لتنفيذ هذا الإجراء. هذه العملية مقتصرة على مالك مساحة العمل.");
            }
        }

        // 2. Load target member
        var member = await _unitOfWork.WorkspaceMembers.FirstOrDefaultAsync(
            m => m.Id == request.MemberId && m.WorkspaceId == request.WorkspaceId,
            cancellationToken);

        if (member == null)
        {
            return Error.NotFound("MEMBER_NOT_FOUND", "عضو طاقم العمل المطلوب غير موجود في مساحة العمل هذه.");
        }

        // 3. Safety check: Owner cannot be deactivated
        if (member.Role == "Owner")
        {
            return Error.Validation("CANNOT_DEACTIVATE_OWNER", "لا يمكن إلغاء تفعيل مالك مساحة العمل.");
        }

        var now = _dateTimeProvider.UtcNow;
        member.Deactivate(now);
        _unitOfWork.WorkspaceMembers.Update(member);

        // 4. Security requirement SC-03: Immediately revoke all active refresh tokens for the deactivated user
        var activeTokens = await _unitOfWork.RefreshTokens.FindAsync(
            t => t.UserId == member.UserId && t.RevokedAt == null && t.ExpiresAt > now,
            cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(now);
            _unitOfWork.RefreshTokens.Update(token);
        }

        // 5. Audit log
        await _auditLogService.LogAsync(
            actorUserId: currentUserId.Value,
            workspaceId: request.WorkspaceId,
            action: "MEMBER_DEACTIVATED",
            entityType: "WorkspaceMember",
            entityId: member.Id.ToString(),
            metadata: new { DeactivatedUserId = member.UserId, LeftAt = member.LeftAt },
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DeactivateWorkspaceMemberResultDto>.Success(
            member.Adapt<DeactivateWorkspaceMemberResultDto>());
    }
}
