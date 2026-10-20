using FluentValidation;

namespace Tahdeera.Application.Features.Groups.Commands.GroupWorkspaceAccess;

public sealed class GrantGroupWorkspaceAccessCommandValidator : AbstractValidator<GrantGroupWorkspaceAccessCommand>
{
    public GrantGroupWorkspaceAccessCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required.");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");

        RuleFor(x => x.GranteeWorkspaceId)
            .NotEmpty().WithMessage("Grantee Workspace ID is required.");

        RuleFor(x => x.ActorUserId)
            .NotEmpty().WithMessage("Actor User ID is required.");
    }
}
