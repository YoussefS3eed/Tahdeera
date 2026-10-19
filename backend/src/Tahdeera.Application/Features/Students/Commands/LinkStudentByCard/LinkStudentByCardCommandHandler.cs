using Mapster;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Utilities;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Students.Commands.LinkStudentByCard;

public class LinkStudentByCardCommandHandler : IRequestHandler<LinkStudentByCardCommand, Result<StudentEnrollmentDto>>
{
    private readonly IStudentCredentialRepository _credentialRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IWorkspaceStudentRepository _workspaceStudentRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IGradeRepository _gradeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICredentialSecurityService _securityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LinkStudentByCardCommandHandler(
        IUnitOfWork unitOfWork,
        ICredentialSecurityService securityService,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider)
        : this(
            unitOfWork.StudentCredentials,
            unitOfWork.Students,
            unitOfWork.WorkspaceStudents,
            unitOfWork.SubscriptionPlans,
            unitOfWork.AcademicYears,
            unitOfWork.Grades,
            unitOfWork,
            securityService,
            currentUserService,
            auditLogService,
            dateTimeProvider)
    {
    }

    public LinkStudentByCardCommandHandler(
        IStudentCredentialRepository credentialRepository,
        IStudentRepository studentRepository,
        IWorkspaceStudentRepository workspaceStudentRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        IAcademicYearRepository academicYearRepository,
        IGradeRepository gradeRepository,
        IUnitOfWork unitOfWork,
        ICredentialSecurityService securityService,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider)
    {
        _credentialRepository = credentialRepository;
        _studentRepository = studentRepository;
        _workspaceStudentRepository = workspaceStudentRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _academicYearRepository = academicYearRepository;
        _gradeRepository = gradeRepository;
        _unitOfWork = unitOfWork;
        _securityService = securityService;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<StudentEnrollmentDto>> Handle(LinkStudentByCardCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var now = _dateTimeProvider.UtcNow;

        // 1. Layer 2: Workspace Membership check (Defense-in-depth)
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

        var tokenHash = _securityService.HashToken(request.ScannedToken);

        // 2. Locate active credential matching token hash
        var credential = await _credentialRepository
            .GetActiveByTokenHashAsync(tokenHash, cancellationToken);

        if (credential == null)
        {
            return Error.Validation("INVALID_OR_REVOKED_CARD", "هذا الكارت غير صالح أو تم إلغاؤه مسبقاً.");
        }

        var student = await _studentRepository.GetByIdAsync(credential.StudentId, cancellationToken);
        if (student == null || !student.IsActive)
        {
            return Error.Validation("INVALID_OR_REVOKED_CARD", "هذا الكارت غير صالح أو تم إلغاؤه مسبقاً.");
        }

        var studentId = credential.StudentId;

        // 3. Check if student is already enrolled in this workspace
        var alreadyEnrolled = await _workspaceStudentRepository
            .IsStudentEnrolledAsync(request.WorkspaceId, studentId, cancellationToken);

        if (alreadyEnrolled)
        {
            return Error.Conflict("STUDENT_ALREADY_ENROLLED_IN_WORKSPACE", "هذا الطالب مسجل بالفعل في هذه المساحة التعليمية.");
        }

        // 4. Check workspace active student quota & subscription status
        var subscriptions = await _unitOfWork.WorkspaceSubscriptions
            .FindAsync(s => s.WorkspaceId == request.WorkspaceId, cancellationToken);
        var subscription = subscriptions.OrderByDescending(s => s.EndsAt).FirstOrDefault();

        int maxStudents = int.MaxValue;
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
                maxStudents = subscription.MaxStudentsOverride.Value;
            }
            else if (subscription.PlanId.HasValue)
            {
                var plan = await _subscriptionPlanRepository.GetByIdAsync(subscription.PlanId.Value, cancellationToken);
                if (plan != null)
                {
                    maxStudents = plan.MaxStudents;
                }
            }
        }

        var activeCount = await _workspaceStudentRepository
            .GetActiveStudentCountAsync(request.WorkspaceId, cancellationToken);

        if (activeCount >= maxStudents)
        {
            return Error.Unprocessable("QUOTA_MAX_STUDENTS_REACHED", "تم الوصول إلى الحد الأقصى لعدد الطلاب المسموح به في باقة اشتراكك الحالية. يرجى ترقية الباقة لإضافة المزيد من الطلاب.");
        }

        var normalizedGuardianPhone = PhoneNumberNormalizer.NormalizeEgyptian(request.GuardianPhone);
        var normalizedStudentPhone = PhoneNumberNormalizer.NormalizeEgyptian(request.StudentPhone);

        // 5. Sibling Phone Collision Guard (HIGH Issue I3)
        if (!request.AllowSiblingPhone)
        {
            var phoneExists = await _workspaceStudentRepository
                .GuardianPhoneExistsInWorkspaceAsync(request.WorkspaceId, normalizedGuardianPhone, cancellationToken: cancellationToken);

            if (phoneExists)
            {
                return Error.Conflict("GUARDIAN_PHONE_ALREADY_EXISTS_CONFIRM_SIBLING", "رقم هاتف ولي الأمر مسجل بالفعل لطالب آخر في هذا السنتر. يرجى تأكيد تسجيل أخ/أخت للمتابعة.");
            }
        }

        // 6. Resolve active academic year and grade
        var activeYear = await _academicYearRepository
            .GetCurrentYearAsync(cancellationToken);

        if (activeYear == null)
        {
            return Error.Validation("ACTIVE_ACADEMIC_YEAR_NOT_CONFIGURED", "لم يتم تهيئة عام دراسي نشط في النظام حالياً. يرجى مراجعة إدارة المنصة.");
        }

        short resolvedGradeId;
        var existingHistory = await _unitOfWork.StudentAcademicHistories
            .FirstOrDefaultAsync(h => h.StudentId == studentId && h.AcademicYearId == activeYear.Id, cancellationToken);

        if (existingHistory != null)
        {
            resolvedGradeId = existingHistory.GradeId;
        }
        else
        {
            if (!request.GradeId.HasValue)
            {
                return Error.Validation("GRADE_REQUIRED_FOR_NEW_ACADEMIC_YEAR", "يجب تحديد المرحلة الدراسية للسنة الدراسية الجديدة لنقل الطالب.");
            }

            resolvedGradeId = request.GradeId.Value;
            var newHistory = new StudentAcademicHistory
            {
                StudentId = studentId,
                AcademicYearId = activeYear.Id,
                GradeId = resolvedGradeId,
                RecordedByUserId = currentUserId != Guid.Empty ? currentUserId : null,
                RecordedAt = now
            };
            await _unitOfWork.StudentAcademicHistories.AddAsync(newHistory, cancellationToken);
        }

        // 7. Create local WorkspaceStudent enrollment
        var workspaceStudent = new WorkspaceStudent
        {
            WorkspaceId = request.WorkspaceId,
            StudentId = studentId,
            Status = WorkspaceStudentStatus.Active,
            GuardianName = request.GuardianName?.Trim(),
            GuardianPhone = normalizedGuardianPhone,
            StudentPhone = normalizedStudentPhone,
            WhatsappOptIn = request.WhatsappOptIn,
            WhatsappOptInAt = request.WhatsappOptIn ? now : null,
            SeatNumber = request.SeatNumber?.Trim(),
            LocalNotes = request.LocalNotes?.Trim(),
            CreatedByUserId = currentUserId != Guid.Empty ? currentUserId : null,
            UpdatedAt = now
        };
        await _workspaceStudentRepository.AddAsync(workspaceStudent, cancellationToken);

        var grade = await _gradeRepository.GetByIdAsync(resolvedGradeId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            currentUserId != Guid.Empty ? currentUserId : null,
            request.WorkspaceId,
            "Student.LinkedByCard",
            "Student",
            studentId.ToString(),
            new { credentialId = credential.Id, gradeId = resolvedGradeId },
            cancellationToken);

        var dto = workspaceStudent.Adapt<StudentEnrollmentDto>() with
        {
            StudentCode = student.StudentCode,
            FullName = student.FullName,
            GradeId = resolvedGradeId,
            GradeNameAr = grade?.NameAr ?? string.Empty,
            GradeNameEn = grade?.NameEn ?? string.Empty,
            AssignedCredentialId = credential.Id
        };

        return Result.Success(dto);
    }
}
