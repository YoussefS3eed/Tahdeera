using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Credentials.Commands.RevokeCredential;

public record RevokeCredentialCommand(
    Guid WorkspaceId,
    Guid CredentialId,
    string Reason
) : IRequest<Result>, ITransactionalRequest;
