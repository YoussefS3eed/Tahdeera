using FluentValidation;

namespace Tahdeera.Application.Features.Workspaces.Commands.SuspendWorkspace;

public class SuspendWorkspaceCommandValidator : AbstractValidator<SuspendWorkspaceCommand>
{
    public SuspendWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("معرف مساحة العمل مطلوب.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("سبب الإيقاف مطلوب.")
            .MaximumLength(500).WithMessage("سبب الإيقاف لا يجب أن يتجاوز 500 حرف.");
    }
}
