using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Groups.Commands.WithdrawStudentFromGroup;

public sealed record WithdrawStudentFromGroupCommand(
    Guid WorkspaceId,
    Guid GroupId,
    Guid StudentId,
    string? Reason,
    Guid ActorUserId) : IRequest<Result>, ITransactionalRequest;
