using Mapster;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Utilities;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Students.Commands.RegisterStudent;

public class RegisterStudentCommandHandler : IRequestHandler<RegisterStudentCommand, Result<StudentEnrollmentDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IWorkspaceStudentRepository _workspaceStudentRepository;
    private readonly IStudentCredentialRepository _credentialRepository;
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IGradeRepository _gradeRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICredentialSecurityService _securityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterStudentCommandHandler(
        IUnitOfWork unitOfWork,
        ICredentialSecurityService securityService,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider)
        : this(
            unitOfWork.Students,
            unitOfWork.WorkspaceStudents,
            unitOfWork.StudentCredentials,
            unitOfWork.AcademicYears,
            unitOfWork.Grades,
            unitOfWork.SubscriptionPlans,
            unitOfWork,
            securityService,
            currentUserService,
            auditLogService,
            dateTimeProvider)
    {
    }

    public RegisterStudentCommandHandler(
        IStudentRepository studentRepository,
        IWorkspaceStudentRepository workspaceStudentRepository,
        IStudentCredentialRepository credentialRepository,
        IAcademicYearRepository academicYearRepository,
        IGradeRepository gradeRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        IUnitOfWork unitOfWork,
        ICredentialSecurityService securityService,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider)
    {
        _studentRepository = studentRepository;
        _workspaceStudentRepository = workspaceStudentRepository;
        _credentialRepository = credentialRepository;
        _academicYearRepository = academicYearRepository;
        _gradeRepository = gradeRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _unitOfWork = unitOfWork;
        _securityService = securityService;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<StudentEnrollmentDto>> Handle(RegisterStudentCommand request, CancellationToken cancellationToken)
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

        var normalizedGuardianPhone = PhoneNumberNormalizer.NormalizeEgyptian(request.GuardianPhone);
        var normalizedStudentPhone = PhoneNumberNormalizer.NormalizeEgyptian(request.StudentPhone);

        // 2. Verify active academic year exists
        var activeYear = await _academicYearRepository
            .GetCurrentYearAsync(cancellationToken);

        if (activeYear == null)
        {
            return Error.Validation("ACTIVE_ACADEMIC_YEAR_NOT_CONFIGURED", "لم يتم تهيئة عام دراسي نشط في النظام حالياً. يرجى مراجعة إدارة المنصة.");
        }

        // 3. Check workspace active student quota & subscription status
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

        // 4. Check duplicate guardian phone in same workspace if sibling confirmation not enabled
        if (!request.AllowSiblingPhone)
        {
            var phoneConflict = await _workspaceStudentRepository
                .GuardianPhoneExistsInWorkspaceAsync(request.WorkspaceId, normalizedGuardianPhone, null, cancellationToken);

            if (phoneConflict)
            {
                return Error.Conflict("GUARDIAN_PHONE_ALREADY_EXISTS_CONFIRM_SIBLING", "رقم هاتف ولي الأمر مسجل بالفعل لطالب آخر في هذا المركز. يرجى تأكيد تسجيل أخ/أخت للمتابعة.");
            }
        }

        // 5. Check card token collision if supplied
        byte[]? tokenHash = null;
        if (!string.IsNullOrWhiteSpace(request.CardToken))
        {
            tokenHash = _securityService.HashToken(request.CardToken);
            var cardConflict = await _credentialRepository
                .TokenHashExistsAsync(tokenHash, cancellationToken);

            if (cardConflict)
            {
                return Error.Conflict("CREDENTIAL_TOKEN_ALREADY_EXISTS", "هذا الكارت مسجل بالفعل في النظام لبطاقة أخرى. يرجى مسح كارت جديد فارغ.");
            }
        }

        // 6. Generate collision-resilient student code
        var currentYear = activeYear.StartsOn.Year;
        string studentCode = string.Empty;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidateCode = _securityService.GenerateStudentCode(currentYear);
            var exists = await _studentRepository.ExistsByCodeAsync(candidateCode, cancellationToken);
            if (!exists)
            {
                studentCode = candidateCode;
                break;
            }
        }

        if (string.IsNullOrEmpty(studentCode))
        {
            studentCode = _securityService.GenerateStudentCode(currentYear);
        }

        // 7. Create Student
        var student = new Student
        {
            StudentCode = studentCode,
            FullName = request.FullName.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _studentRepository.AddAsync(student, cancellationToken);

        // 8. Record Academic History
        var history = new StudentAcademicHistory
        {
            StudentId = student.Id,
            AcademicYearId = activeYear.Id,
            GradeId = request.GradeId,
            RecordedByUserId = currentUserId != Guid.Empty ? currentUserId : null,
            RecordedAt = now
        };
        await _unitOfWork.StudentAcademicHistories.AddAsync(history, cancellationToken);

        // 9. Create WorkspaceStudent
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

        // 10. Create NFC credential if cardToken was supplied
        Guid? assignedCredentialId = null;
        if (tokenHash != null)
        {
            var credential = new StudentCredential
            {
                StudentId = student.Id,
                WorkspaceId = request.WorkspaceId,
                CredentialType = CredentialType.Nfc,
                TokenHash = tokenHash,
                CardUidDisplay = request.CardUidDisplay?.Trim(),
                IssuedAt = now,
                IssuedByUserId = currentUserId
            };
            await _credentialRepository.AddAsync(credential, cancellationToken);
            assignedCredentialId = credential.Id;
        }

        // 11. Query Grade details for DTO
        var grade = await _gradeRepository.GetByIdAsync(request.GradeId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            currentUserId != Guid.Empty ? currentUserId : null,
            request.WorkspaceId,
            "Student.Registered",
            "Student",
            student.Id.ToString(),
            new { studentCode, request.GradeId },
            cancellationToken);

        var dto = workspaceStudent.Adapt<StudentEnrollmentDto>() with
        {
            StudentCode = student.StudentCode,
            FullName = student.FullName,
            GradeId = request.GradeId,
            GradeNameAr = grade?.NameAr ?? string.Empty,
            GradeNameEn = grade?.NameEn ?? string.Empty,
            AssignedCredentialId = assignedCredentialId
        };

        return Result.Success(dto);
    }
}
