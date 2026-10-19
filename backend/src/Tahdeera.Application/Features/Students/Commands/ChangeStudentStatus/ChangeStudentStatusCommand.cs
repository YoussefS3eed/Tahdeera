using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Students.Commands.ChangeStudentStatus;

public record ChangeStudentStatusCommand(
    Guid WorkspaceId,
    Guid StudentId,
    string Status,
    string? Reason = null
) : MediatR.IRequest<Result>, ITransactionalRequest;
