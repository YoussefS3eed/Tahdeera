using MediatR;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Students.Queries.GetStudentDetails;

public record GetStudentDetailsQuery(Guid WorkspaceId, Guid StudentId) : IRequest<Result<StudentDetailsDto>>;
