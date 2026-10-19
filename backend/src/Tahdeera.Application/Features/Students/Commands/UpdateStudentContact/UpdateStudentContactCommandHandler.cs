using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Utilities;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Students.Commands.UpdateStudentContact;

public class UpdateStudentContactCommandHandler : IRequestHandler<UpdateStudentContactCommand, Result>
{
    private readonly IWorkspaceStudentRepository _workspaceStudentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;

    public UpdateStudentContactCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService)
        : this(
            unitOfWork.WorkspaceStudents,
            unitOfWork.Students,
            unitOfWork,
            currentUserService,
            auditLogService)
    {
    }

    public UpdateStudentContactCommandHandler(
        IWorkspaceStudentRepository workspaceStudentRepository,
        IStudentRepository studentRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService)
    {
        _workspaceStudentRepository = workspaceStudentRepository;
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
    }

    public async Task<Result> Handle(UpdateStudentContactCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;

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

        // 1. Update Student global full name if provided
        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            if (student != null && request.FullName.Trim() != student.FullName)
            {
                student.UpdateName(request.FullName);
                _studentRepository.Update(student);
            }
        }

        // 2. Normalize phone numbers
        var normalizedGuardianPhone = PhoneNumberNormalizer.NormalizeEgyptian(request.GuardianPhone);
        var normalizedStudentPhone = PhoneNumberNormalizer.NormalizeEgyptian(request.StudentPhone);

        // 3. Sibling guardian phone collision guard
        if (normalizedGuardianPhone is not null &&
            normalizedGuardianPhone != wsStudent.GuardianPhone &&
            !request.AllowSiblingPhone)
        {
            var phoneConflict = await _workspaceStudentRepository
                .GuardianPhoneExistsInWorkspaceAsync(request.WorkspaceId, normalizedGuardianPhone, request.StudentId, cancellationToken);

            if (phoneConflict)
            {
                return Error.Conflict("GUARDIAN_PHONE_ALREADY_EXISTS_CONFIRM_SIBLING", "رقم ولي الأمر مسجل مسبقاً لطالب آخر في هذا المركز. يرجى تأكيد صلة القرابة (أخ/أخت) للمتابعة.");
            }
        }

        // 4. Update workspace contact info
        wsStudent.UpdateContactInfo(
            studentPhone: normalizedStudentPhone,
            guardianName: request.GuardianName,
            guardianPhone: normalizedGuardianPhone,
            whatsappOptIn: request.WhatsappOptIn,
            seatNumber: request.SeatNumber,
            localNotes: request.LocalNotes);

        _workspaceStudentRepository.Update(wsStudent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            currentUserId != Guid.Empty ? currentUserId : null,
            request.WorkspaceId,
            "Student.ContactUpdated",
            "WorkspaceStudent",
            wsStudent.Id.ToString(),
            new { request.StudentId, wsStudent.GuardianPhone, wsStudent.WhatsappOptIn },
            cancellationToken);

        return Result.Success();
    }
}
