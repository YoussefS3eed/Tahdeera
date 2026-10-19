using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Students.Commands.ChangeStudentStatus;

public class ChangeStudentStatusCommandHandler : IRequestHandler<ChangeStudentStatusCommand, Result>
{
    private readonly IWorkspaceStudentRepository _workspaceStudentRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeStudentStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider)
        : this(
            unitOfWork.WorkspaceStudents,
            unitOfWork.SubscriptionPlans,
            unitOfWork,
            currentUserService,
            auditLogService,
            dateTimeProvider)
    {
    }

    public ChangeStudentStatusCommandHandler(
        IWorkspaceStudentRepository workspaceStudentRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider)
    {
        _workspaceStudentRepository = workspaceStudentRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ChangeStudentStatusCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var now = _dateTimeProvider.UtcNow;

        // Layer 2: Workspace Membership check (Defense-in-depth)
        if (!_currentUserService.IsPlatformAdmin && currentUserId != Guid.Empty)
        {
            var isMember = await _unitOfWork.WorkspaceMembers.AnyAsync(
                m => m.WorkspaceId == request.WorkspaceId && m.UserId == currentUserId && m.IsActive && m.LeftAt == null,
                cancellationToken);

            if (!isMember)
            {
                return Error.Forbidden("UNAUTHORIZED_WORKSPACE_ACCESS", "ليس لديك صلاحية الوصول إلى هذه المساحة التعليمية.");
            }
        }

        var wsStudent = await _workspaceStudentRepository
            .GetByWorkspaceAndStudentIdAsync(request.WorkspaceId, request.StudentId, cancellationToken);

        if (wsStudent is null)
        {
            return Error.NotFound("STUDENT_NOT_FOUND_IN_WORKSPACE", "الطالب غير مسجل في مركز الدروس الحالي.");
        }

        if (!Enum.TryParse<WorkspaceStudentStatus>(request.Status, ignoreCase: true, out var newStatus))
        {
            return Error.Validation("INVALID_STUDENT_STATUS", "حالة الطالب غير صالحة.");
        }

        var oldStatus = wsStudent.Status;
        if (oldStatus == newStatus)
        {
            return Result.Success();
        }

        // If transitioning to Active, re-evaluate workspace capacity quota & subscription
        if (newStatus == WorkspaceStudentStatus.Active)
        {
            var subscriptions = await _unitOfWork.WorkspaceSubscriptions
                .FindAsync(s => s.WorkspaceId == request.WorkspaceId, cancellationToken);
            var subscription = subscriptions.OrderByDescending(s => s.EndsAt).FirstOrDefault();

            int effectiveMax = int.MaxValue;
            if (subscription != null)
            {
                if (subscription.ManualSuspendedAt.HasValue)
                {
                    return Error.Forbidden("WORKSPACE_SUSPENDED", "تم إيقاف السنتر بقرار إداري. لا يمكن إجراء عمليات.");
                }

                if (subscription.Status != "Active" || subscription.EndsAt <= now)
                {
                    return Error.Forbidden("WORKSPACE_SUSPENDED", "انتهت مدة اشتراكك في النظام. لا يمكن إجراء عمليات.");
                }

                if (subscription.MaxStudentsOverride.HasValue)
                {
                    effectiveMax = subscription.MaxStudentsOverride.Value;
                }
                else if (subscription.PlanId.HasValue)
                {
                    var plan = await _subscriptionPlanRepository.GetByIdAsync(subscription.PlanId.Value, cancellationToken);
                    if (plan != null)
                    {
                        effectiveMax = plan.MaxStudents;
                    }
                }
            }

            var activeCount = await _workspaceStudentRepository
                .GetActiveStudentCountAsync(request.WorkspaceId, cancellationToken);

            if (activeCount >= effectiveMax)
            {
                return Error.Unprocessable("QUOTA_MAX_STUDENTS_REACHED", "تم الوصول للحد الأقصى لعدد الطلاب المسموح به في اشتراك هذا المركز.");
            }
        }

        wsStudent.SetStatus(newStatus);
        _workspaceStudentRepository.Update(wsStudent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            currentUserId != Guid.Empty ? currentUserId : null,
            request.WorkspaceId,
            "Student.StatusChanged",
            "WorkspaceStudent",
            wsStudent.Id.ToString(),
            new { oldStatus = oldStatus.ToString(), newStatus = newStatus.ToString(), request.Reason },
            cancellationToken);

        return Result.Success();
    }
}
