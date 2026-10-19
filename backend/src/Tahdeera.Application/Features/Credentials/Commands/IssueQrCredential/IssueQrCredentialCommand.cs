using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Credentials.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Credentials.Commands.IssueQrCredential;

public record IssueQrCredentialCommand(
    Guid WorkspaceId,
    Guid StudentId
) : IRequest<Result<QrCredentialIssuedDto>>, ITransactionalRequest;
