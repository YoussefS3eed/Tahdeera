using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Credentials.Commands.RevokeCredential;

public class RevokeCredentialCommandHandler : IRequestHandler<RevokeCredentialCommand, Result>
{
    private readonly IStudentCredentialRepository _credentialRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RevokeCredentialCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider)
        : this(
            unitOfWork.StudentCredentials,
            unitOfWork,
            currentUserService,
            auditLogService,
            dateTimeProvider)
    {
    }

    public RevokeCredentialCommandHandler(
        IStudentCredentialRepository credentialRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider)
    {
        _credentialRepository = credentialRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RevokeCredentialCommand request, CancellationToken cancellationToken)
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

        var credential = await _credentialRepository
            .FirstOrDefaultAsync(c => c.Id == request.CredentialId && c.WorkspaceId == request.WorkspaceId, cancellationToken);

        if (credential == null)
        {
            return Error.NotFound("CREDENTIAL_NOT_FOUND", "لم يتم العثور على الكارت أو الرمز المطلوب.");
        }

        if (credential.RevokedAt != null)
        {
            return Error.Conflict("CREDENTIAL_ALREADY_REVOKED", "تم إلغاء تفعيل هذا الكارت أو الرمز مسبقاً.");
        }

        credential.Revoke(currentUserId, request.Reason, now);
        _credentialRepository.Update(credential);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            currentUserId != Guid.Empty ? currentUserId : null,
            request.WorkspaceId,
            "Credential.Revoked",
            "StudentCredential",
            credential.Id.ToString(),
            new { reason = request.Reason, studentId = credential.StudentId },
            cancellationToken);

        return Result.Success();
    }
}
