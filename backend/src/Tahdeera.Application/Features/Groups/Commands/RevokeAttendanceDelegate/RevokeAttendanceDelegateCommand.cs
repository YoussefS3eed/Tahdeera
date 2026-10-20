using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Groups.Commands.RevokeAttendanceDelegate;

public sealed record RevokeAttendanceDelegateCommand(
    Guid WorkspaceId,
    Guid GroupId,
    Guid DelegationId,
    string Reason,
    Guid ActorUserId) : IRequest<Result>, ITransactionalRequest;
