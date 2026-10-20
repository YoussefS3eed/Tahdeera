using FluentValidation;

namespace Tahdeera.Application.Features.Groups.Commands.ChangeGroupStatus;

public sealed class ChangeGroupStatusCommandValidator : AbstractValidator<ChangeGroupStatusCommand>
{
    public ChangeGroupStatusCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required.");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");

        RuleFor(x => x.ActorUserId)
            .NotEmpty().WithMessage("Actor User ID is required.");

        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Invalid group status.");
    }
}
