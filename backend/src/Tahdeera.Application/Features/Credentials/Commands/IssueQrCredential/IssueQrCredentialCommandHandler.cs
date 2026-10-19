using Mapster;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Credentials.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Credentials.Commands.IssueQrCredential;

public class IssueQrCredentialCommandHandler : IRequestHandler<IssueQrCredentialCommand, Result<QrCredentialIssuedDto>>
{
    private readonly IStudentCredentialRepository _credentialRepository;
    private readonly IWorkspaceStudentRepository _workspaceStudentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICredentialSecurityService _securityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public IssueQrCredentialCommandHandler(
        IUnitOfWork unitOfWork,
        ICredentialSecurityService securityService,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider)
        : this(
            unitOfWork.StudentCredentials,
            unitOfWork.WorkspaceStudents,
            unitOfWork,
            securityService,
            currentUserService,
            auditLogService,
            dateTimeProvider)
    {
    }

    public IssueQrCredentialCommandHandler(
        IStudentCredentialRepository credentialRepository,
        IWorkspaceStudentRepository workspaceStudentRepository,
        IUnitOfWork unitOfWork,
        ICredentialSecurityService securityService,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider)
    {
        _credentialRepository = credentialRepository;
        _workspaceStudentRepository = workspaceStudentRepository;
        _unitOfWork = unitOfWork;
        _securityService = securityService;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<QrCredentialIssuedDto>> Handle(IssueQrCredentialCommand request, CancellationToken cancellationToken)
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

        // 1. Verify student is enrolled in caller workspace via specific repository
        var isEnrolled = await _workspaceStudentRepository
            .IsStudentEnrolledAsync(request.WorkspaceId, request.StudentId, cancellationToken);

        if (!isEnrolled)
        {
            return Error.NotFound("STUDENT_NOT_FOUND_IN_WORKSPACE", "الطالب غير مسجل في هذه المساحة التعليمية.");
        }

        // 2. Generate raw QR payload (tahdeera://qr/{hex32})
        var rawPayload = _securityService.GenerateQrPayload();
        var tokenHash = _securityService.HashToken(rawPayload);

        // 3. Auto-revoke prior active QR credential for student in this workspace
        var existingActiveQr = await _credentialRepository
            .GetActiveCardForStudentAsync(request.WorkspaceId, request.StudentId, CredentialType.Qr, cancellationToken);

        Guid? replacedCredentialId = null;
        if (existingActiveQr != null)
        {
            existingActiveQr.Revoke(currentUserId, "Replaced by new QR code", now);
            _credentialRepository.Update(existingActiveQr);
            replacedCredentialId = existingActiveQr.Id;
        }

        // 4. Create new QR credential
        var newQr = new StudentCredential
        {
            StudentId = request.StudentId,
            WorkspaceId = request.WorkspaceId,
            CredentialType = CredentialType.Qr,
            TokenHash = tokenHash,
            IssuedAt = now,
            IssuedByUserId = currentUserId
        };
        await _credentialRepository.AddAsync(newQr, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            currentUserId != Guid.Empty ? currentUserId : null,
            request.WorkspaceId,
            "Credential.QrIssued",
            "StudentCredential",
            newQr.Id.ToString(),
            new { studentId = request.StudentId, replacedCredentialId },
            cancellationToken);

        var dto = newQr.Adapt<QrCredentialIssuedDto>() with
        {
            RawPayload = rawPayload,
            ReplacedCredentialId = replacedCredentialId
        };

        return Result.Success(dto);
    }
}
