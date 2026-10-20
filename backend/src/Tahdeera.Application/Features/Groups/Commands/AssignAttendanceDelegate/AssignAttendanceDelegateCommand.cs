using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Groups.Commands.AssignAttendanceDelegate;

public sealed record AssignAttendanceDelegateCommand(
    Guid WorkspaceId,
    Guid GroupId,
    Guid AssistantUserId,
    Guid TeacherUserId) : IRequest<Result<GroupDelegationDto>>, ITransactionalRequest;
