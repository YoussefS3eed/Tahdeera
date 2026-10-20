using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Groups.Commands.EnrollStudentInGroup;

public sealed record EnrollStudentInGroupCommand(
    Guid WorkspaceId,
    Guid GroupId,
    Guid StudentId,
    Guid ActorUserId) : IRequest<Result<GroupStudentEnrollmentDto>>, ITransactionalRequest;
