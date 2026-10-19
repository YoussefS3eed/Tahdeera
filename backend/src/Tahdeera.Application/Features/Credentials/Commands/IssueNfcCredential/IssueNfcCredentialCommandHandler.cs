using Mapster;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Credentials.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Credentials.Commands.IssueNfcCredential;

public class IssueNfcCredentialCommandHandler : IRequestHandler<IssueNfcCredentialCommand, Result<CredentialIssuedDto>>
{
    private readonly IStudentCredentialRepository _credentialRepository;
    private readonly IWorkspaceStudentRepository _workspaceStudentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICredentialSecurityService _securityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public IssueNfcCredentialCommandHandler(
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

    public IssueNfcCredentialCommandHandler(
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

    public async Task<Result<CredentialIssuedDto>> Handle(IssueNfcCredentialCommand request, CancellationToken cancellationToken)
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

        // 2. Hash token and check global uniqueness via specific repository
        var tokenHash = _securityService.HashToken(request.Token);
        var tokenExists = await _credentialRepository
            .TokenHashExistsAsync(tokenHash, cancellationToken);

        if (tokenExists)
        {
            return Error.Conflict("CREDENTIAL_TOKEN_ALREADY_EXISTS", "هذا الكارت مسجل بالفعل في النظام لبطاقة أخرى. يرجى مسح كارت جديد فارغ.");
        }

        // 3. Single Active NFC Card Rule: Revoke any existing active NFC card for this student in this workspace
        var existingActiveCard = await _credentialRepository
            .GetActiveCardForStudentAsync(request.WorkspaceId, request.StudentId, CredentialType.Nfc, cancellationToken);

        Guid? replacedCardId = null;
        if (existingActiveCard != null)
        {
            existingActiveCard.Revoke(currentUserId, "Replaced by new card issuance", now);
            _credentialRepository.Update(existingActiveCard);
            replacedCardId = existingActiveCard.Id;
        }

        // 4. Create new NFC credential
        var newCard = new StudentCredential
        {
            StudentId = request.StudentId,
            WorkspaceId = request.WorkspaceId,
            CredentialType = CredentialType.Nfc,
            TokenHash = tokenHash,
            CardUidDisplay = request.CardUidDisplay?.Trim(),
            IssuedByUserId = currentUserId,
            IssuedAt = now
        };

        await _credentialRepository.AddAsync(newCard, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            currentUserId,
            request.WorkspaceId,
            "Credential.NfcIssued",
            "StudentCredential",
            newCard.Id.ToString(),
            new { request.StudentId, replacedCardId },
            cancellationToken);

        var dto = newCard.Adapt<CredentialIssuedDto>() with { ReplacedCredentialId = replacedCardId };

        return Result.Success(dto);
    }
}
