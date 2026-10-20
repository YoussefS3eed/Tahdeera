using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Groups.Commands.ChangeGroupStudentStatus;

public sealed record ChangeGroupStudentStatusCommand(
    Guid WorkspaceId,
    Guid GroupId,
    Guid StudentId,
    GroupStudentStatus NewStatus,
    string Reason,
    Guid ActorUserId) : IRequest<Result>, ITransactionalRequest;
