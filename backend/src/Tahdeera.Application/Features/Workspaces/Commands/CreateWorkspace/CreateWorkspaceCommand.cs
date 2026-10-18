using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;

public record CreateWorkspaceCommand(
    string Name,
    string Kind,
    string? TimezoneId,
    string? ContactPhone,
    string? CityAddress,
    int? DefaultLateGraceMinutes,
    CreateOwnerDto Owner,
    CreateInitialSubscriptionDto Subscription) : IRequest<Result<CreateWorkspaceResultDto>>, ITransactionalRequest;
