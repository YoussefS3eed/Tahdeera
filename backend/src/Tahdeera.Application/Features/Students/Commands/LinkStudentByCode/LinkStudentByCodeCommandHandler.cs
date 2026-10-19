using Mapster;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Utilities;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Students.Commands.LinkStudentByCode;

public class LinkStudentByCodeCommandHandler : IRequestHandler<LinkStudentByCodeCommand, Result<StudentEnrollmentDto>>
{
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

    public LinkStudentByCodeCommandHandler(
        IUnitOfWork unitOfWork,
        ICredentialSecurityService securityService,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider)
        : this(
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

    public LinkStudentByCodeCommandHandler(
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

    public async Task<Result<StudentEnrollmentDto>> Handle(LinkStudentByCodeCommand request, CancellationToken cancellationToken)
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

        var normalizedCode = request.StudentCode.Trim().ToUpperInvariant();
        var rawFactor = request.VerificationFactor.Trim();
        var normalizedPhoneFactor = PhoneNumberNormalizer.NormalizeEgyptian(rawFactor);

        // 2. Query student by StudentCode with eager-loaded histories & enrollments (single optimal DB query)
        var student = await _studentRepository
            .GetByCodeAsync(normalizedCode, cancellationToken);

        bool isVerified = false;

        if (student != null && student.IsActive)
        {
            // Verification Factor 1: PIN hash match (if PIN configured)
            if (student.VerificationPinHash != null)
            {
                var factorHash = _securityService.HashToken(rawFactor);
                if (student.VerificationPinHash.SequenceEqual(factorHash))
                {
                    isVerified = true;
                }
            }

            // Verification Factor 2: Registered Guardian Phone or Student Phone match in any existing workspace enrollment
            if (!isVerified && student.WorkspaceEnrollments != null && student.WorkspaceEnrollments.Count > 0)
            {
                var matchesPhone = student.WorkspaceEnrollments.Any(we =>
                    (!string.IsNullOrEmpty(we.GuardianPhone) && (we.GuardianPhone == normalizedPhoneFactor || we.GuardianPhone == rawFactor)) ||
                    (!string.IsNullOrEmpty(we.StudentPhone) && (we.StudentPhone == normalizedPhoneFactor || we.StudentPhone == rawFactor)));

                if (matchesPhone)
                {
                    isVerified = true;
                }
            }
        }

        // Anti-Enumeration Defense: Execute dummy verification delay and return uniform error
        if (student == null || !isVerified)
        {
            await _securityService.ExecuteDummyVerificationDelayAsync(cancellationToken);
            return Error.Validation("STUDENT_ENROLLMENT_VERIFICATION_FAILED", "تعذر التحقق من بيانات الطالب. كود الطالب أو رقم هاتف ولي الأمر غير متطابق.");
        }

        // 3. Check if already enrolled in this workspace
        var alreadyEnrolled = await _workspaceStudentRepository
            .IsStudentEnrolledAsync(request.WorkspaceId, student.Id, cancellationToken);

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
        if (!request.AllowSiblingPhone && !string.IsNullOrWhiteSpace(normalizedGuardianPhone))
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
            .FirstOrDefaultAsync(h => h.StudentId == student.Id && h.AcademicYearId == activeYear.Id, cancellationToken);

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
                StudentId = student.Id,
                AcademicYearId = activeYear.Id,
                GradeId = resolvedGradeId,
                RecordedByUserId = currentUserId != Guid.Empty ? currentUserId : null,
                RecordedAt = now
            };
            await _unitOfWork.StudentAcademicHistories.AddAsync(newHistory, cancellationToken);
        }

        // 7. Create WorkspaceStudent record for caller workspace
        var workspaceStudent = new WorkspaceStudent
        {
            WorkspaceId = request.WorkspaceId,
            StudentId = student.Id,
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
            "Student.LinkedByCode",
            "Student",
            student.Id.ToString(),
            new { gradeId = resolvedGradeId },
            cancellationToken);

        var dto = workspaceStudent.Adapt<StudentEnrollmentDto>() with
        {
            StudentCode = student.StudentCode,
            FullName = student.FullName,
            GradeId = resolvedGradeId,
            GradeNameAr = grade?.NameAr ?? string.Empty,
            GradeNameEn = grade?.NameEn ?? string.Empty,
            AssignedCredentialId = null
        };

        return Result.Success(dto);
    }
}
