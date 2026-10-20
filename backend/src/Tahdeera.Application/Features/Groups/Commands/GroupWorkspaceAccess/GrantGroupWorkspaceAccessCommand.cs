using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Groups.Commands.GroupWorkspaceAccess;

public sealed record GrantGroupWorkspaceAccessCommand(
    Guid WorkspaceId,
    Guid GroupId,
    Guid GranteeWorkspaceId,
    bool CanViewRoster,
    bool CanRegisterAttendance,
    bool CanEditAttendance,
    DateTimeOffset? ExpiresAt,
    Guid ActorUserId,
    string? Notes = null) : IRequest<Result<GroupWorkspaceAccessDto>>, ITransactionalRequest;
