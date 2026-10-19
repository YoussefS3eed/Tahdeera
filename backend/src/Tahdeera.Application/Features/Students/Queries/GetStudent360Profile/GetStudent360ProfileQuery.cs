using MediatR;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Students.Queries.GetStudent360Profile;

public record GetStudent360ProfileQuery(Guid WorkspaceId, Guid StudentId) : IRequest<Result<Student360ProfileDto>>;
