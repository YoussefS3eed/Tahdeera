using Mapster;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Credentials.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Credentials.Queries.GetStudentCredentials;

public class GetStudentCredentialsQueryHandler : IRequestHandler<GetStudentCredentialsQuery, Result<IReadOnlyList<CredentialDetailDto>>>
{
    private readonly IStudentCredentialRepository _credentialRepository;
    private readonly IWorkspaceStudentRepository? _workspaceStudentRepository;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IUnitOfWork? _unitOfWork;

    public GetStudentCredentialsQueryHandler(
        IStudentCredentialRepository credentialRepository,
        IWorkspaceStudentRepository? workspaceStudentRepository = null,
        ICurrentUserService? currentUserService = null,
        IUnitOfWork? unitOfWork = null)
    {
        _credentialRepository = credentialRepository;
        _workspaceStudentRepository = workspaceStudentRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<CredentialDetailDto>>> Handle(GetStudentCredentialsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService?.UserId ?? Guid.Empty;

        // Layer 2: Workspace Membership check (Defense-in-depth)
        if (_unitOfWork != null && _currentUserService != null && !_currentUserService.IsPlatformAdmin && currentUserId != Guid.Empty)
        {
            var isMember = await _unitOfWork.WorkspaceMembers.AnyAsync(
                m => m.WorkspaceId == request.WorkspaceId && m.UserId == currentUserId && m.IsActive && m.LeftAt == null,
                cancellationToken);

            if (!isMember)
            {
                return Error.Forbidden("UNAUTHORIZED_WORKSPACE_ACCESS", "ليس لديك صلاحية الوصول إلى هذه المساحة التعليمية.");
            }
        }

        // Verify student is enrolled in caller workspace (Defect U2)
        if (_workspaceStudentRepository != null)
        {
            var isEnrolled = await _workspaceStudentRepository
                .IsStudentEnrolledAsync(request.WorkspaceId, request.StudentId, cancellationToken);

            if (!isEnrolled)
            {
                return Error.NotFound("STUDENT_NOT_FOUND_IN_WORKSPACE", "الطالب غير مسجل في هذه المساحة التعليمية.");
            }
        }

        var credentials = await _credentialRepository.GetCredentialsByStudentAsync(
            request.WorkspaceId,
            request.StudentId,
            includeUserDetails: true,
            cancellationToken);

        return Result.Success<IReadOnlyList<CredentialDetailDto>>(credentials.Adapt<List<CredentialDetailDto>>());
    }
}
