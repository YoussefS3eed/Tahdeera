using FluentValidation;

namespace Tahdeera.Application.Features.Credentials.Commands.IssueNfcCredential;

public class IssueNfcCredentialCommandValidator : AbstractValidator<IssueNfcCredentialCommand>
{
    public IssueNfcCredentialCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(500);

        When(x => !string.IsNullOrWhiteSpace(x.CardUidDisplay), () =>
        {
            RuleFor(x => x.CardUidDisplay!)
                .MaximumLength(64);
        });
    }
}
