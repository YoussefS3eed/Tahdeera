using MediatR;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Groups.Queries.GetGroupDetails;

public sealed record GetGroupDetailsQuery(
    Guid WorkspaceId,
    Guid GroupId) : IRequest<Result<GroupDetailDto>>;
