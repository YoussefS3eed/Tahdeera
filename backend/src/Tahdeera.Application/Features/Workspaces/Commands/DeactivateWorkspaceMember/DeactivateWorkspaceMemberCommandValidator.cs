using FluentValidation;

namespace Tahdeera.Application.Features.Workspaces.Commands.DeactivateWorkspaceMember;

public class DeactivateWorkspaceMemberCommandValidator : AbstractValidator<DeactivateWorkspaceMemberCommand>
{
    public DeactivateWorkspaceMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("معرف مساحة العمل مطلوب.");

        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage("معرف عضو طاقم العمل مطلوب.");
    }
}
