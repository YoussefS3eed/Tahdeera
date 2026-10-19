using FluentValidation;

namespace Tahdeera.Application.Features.Credentials.Commands.RevokeCredential;

public class RevokeCredentialCommandValidator : AbstractValidator<RevokeCredentialCommand>
{
    public RevokeCredentialCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.CredentialId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Revocation reason is required.")
            .MaximumLength(256)
            .WithMessage("Revocation reason cannot exceed 256 characters.");
    }
}
