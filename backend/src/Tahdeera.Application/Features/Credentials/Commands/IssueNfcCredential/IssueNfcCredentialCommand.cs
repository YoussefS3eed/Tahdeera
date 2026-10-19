using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Credentials.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Credentials.Commands.IssueNfcCredential;

public record IssueNfcCredentialCommand(
    Guid WorkspaceId,
    Guid StudentId,
    string Token,
    string? CardUidDisplay
) : IRequest<Result<CredentialIssuedDto>>, ITransactionalRequest;
