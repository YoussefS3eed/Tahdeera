using MediatR;
using Tahdeera.Application.Features.Credentials.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Credentials.Queries.GetStudentCredentials;

public record GetStudentCredentialsQuery(
    Guid WorkspaceId,
    Guid StudentId
) : IRequest<Result<IReadOnlyList<CredentialDetailDto>>>;
