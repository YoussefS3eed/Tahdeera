using MediatR;
using System.Text.RegularExpressions;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Workspaces.Commands.AddWorkspaceMember;

public class AddWorkspaceMemberCommandHandler : IRequestHandler<AddWorkspaceMemberCommand, Result<AddWorkspaceMemberResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddWorkspaceMemberCommandHandler(
        IUnitOfWork unitOfWork,
        IIdentityService identityService,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _identityService = identityService;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AddWorkspaceMemberResultDto>> Handle(AddWorkspaceMemberCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Error.Unauthorized("AUTH_UNAUTHORIZED", "User is not authenticated.");
        }

        // 1. Role verification: Only Workspace Owner or Platform Admin can add staff
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

        // 2. Load latest subscription and calculate effective max staff quota
        var subscriptions = await _unitOfWork.WorkspaceSubscriptions
            .FindAsync(s => s.WorkspaceId == request.WorkspaceId, cancellationToken);
        var latestSub = subscriptions.OrderByDescending(s => s.EndsAt).FirstOrDefault();

        int maxStaffQuota = 1;
        if (latestSub != null)
        {
            if (latestSub.MaxStaffOverride.HasValue)
            {
                maxStaffQuota = latestSub.MaxStaffOverride.Value;
            }
            else if (latestSub.PlanId.HasValue)
            {
                var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(latestSub.PlanId.Value, cancellationToken);
                if (plan != null)
                {
                    maxStaffQuota = plan.MaxStaff;
                }
            }
        }

        // 3. Count currently active non-owner staff members
        var activeStaffCount = await _unitOfWork.WorkspaceMembers.CountAsync(
            m => m.WorkspaceId == request.WorkspaceId && m.IsActive && m.Role != "Owner",
            cancellationToken);

        if (activeStaffCount >= maxStaffQuota)
        {
            return Error.Unprocessable("QUOTA_MAX_STAFF_REACHED", "تم الوصول إلى الحد الأقصى لطاقم العمل المسموح به في باقتك الحالية.");
        }

        // 4. Check if user already exists globally
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var trimmedUsername = request.Username.Trim();

        var existingUser = await _unitOfWork.AppUsers.FirstOrDefaultAsync(
            u => u.NormalizedEmail == normalizedEmail || u.Username == trimmedUsername,
            cancellationToken);

        Guid memberUserId;
        string displayName;
        var joinedAt = _dateTimeProvider.UtcNow;
        Guid memberId;

        if (existingUser != null)
        {
            memberUserId = existingUser.Id;
            displayName = existingUser.DisplayName;

            // Check if user is already a member of this workspace
            var existingMember = await _unitOfWork.WorkspaceMembers.FirstOrDefaultAsync(
                m => m.WorkspaceId == request.WorkspaceId && m.UserId == existingUser.Id,
                cancellationToken);

            if (existingMember != null)
            {
                if (existingMember.IsActive)
                {
                    return Error.Conflict("USER_ALREADY_MEMBER", "المستخدم مسجل بالفعل كعضو نشط في مساحة العمل هذه.");
                }

                existingMember.Reactivate();
                existingMember.Role = request.Role;
                existingMember.JoinedAt = joinedAt;
                _unitOfWork.WorkspaceMembers.Update(existingMember);
                memberId = existingMember.Id;
            }
            else
            {
                var newMember = new WorkspaceMember
                {
                    WorkspaceId = request.WorkspaceId,
                    UserId = existingUser.Id,
                    Role = request.Role,
                    IsActive = true,
                    JoinedAt = joinedAt,
                    CreatedByUserId = currentUserId.Value
                };
                await _unitOfWork.WorkspaceMembers.AddAsync(newMember, cancellationToken);
                memberId = newMember.Id;
            }
        }
        else
        {
            // Normalize Egyptian phone number if provided
            string? normalizedPhone = null;
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                var trimmedPhone = request.Phone.Trim();
                if (Regex.IsMatch(trimmedPhone, @"^01[0-2,5]\d{8}$"))
                {
                    normalizedPhone = "+20" + trimmedPhone[1..];
                }
                else
                {
                    normalizedPhone = trimmedPhone;
                }
            }

            // Create Identity User
            var identityResult = await _identityService.CreateUserAsync(
                trimmedUsername,
                request.Email.Trim(),
                request.Password,
                isPlatformAdmin: false,
                cancellationToken: cancellationToken);

            if (!identityResult.Success)
            {
                return Error.Validation("IDENTITY_CREATION_FAILED", string.Join("; ", identityResult.Errors));
            }

            var appUser = new AppUser
            {
                Id = identityResult.UserId,
                IdentitySubject = identityResult.UserId.ToString(),
                Username = trimmedUsername,
                DisplayName = request.DisplayName.Trim(),
                NormalizedEmail = normalizedEmail,
                Phone = normalizedPhone,
                PreferredLanguage = "ar",
                PreferredTheme = "light",
                IsPlatformAdmin = false,
                IsActive = true
            };
            await _unitOfWork.AppUsers.AddAsync(appUser, cancellationToken);

            var newMember = new WorkspaceMember
            {
                WorkspaceId = request.WorkspaceId,
                UserId = appUser.Id,
                Role = request.Role,
                IsActive = true,
                JoinedAt = joinedAt,
                CreatedByUserId = currentUserId.Value
            };
            await _unitOfWork.WorkspaceMembers.AddAsync(newMember, cancellationToken);
            memberUserId = appUser.Id;
            displayName = appUser.DisplayName;
            memberId = newMember.Id;
        }

        // 5. Security audit logging
        await _auditLogService.LogAsync(
            actorUserId: currentUserId.Value,
            workspaceId: request.WorkspaceId,
            action: "MEMBER_ADDED",
            entityType: "WorkspaceMember",
            entityId: memberId.ToString(),
            metadata: new { MemberUserId = memberUserId, Role = request.Role },
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AddWorkspaceMemberResultDto>.Success(new AddWorkspaceMemberResultDto(
            memberId,
            memberUserId,
            displayName,
            request.Role,
            true,
            joinedAt));
    }
}
