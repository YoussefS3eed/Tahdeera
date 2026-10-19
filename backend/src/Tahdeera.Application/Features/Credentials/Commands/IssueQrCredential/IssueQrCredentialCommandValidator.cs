using FluentValidation;

namespace Tahdeera.Application.Features.Credentials.Commands.IssueQrCredential;

public class IssueQrCredentialCommandValidator : AbstractValidator<IssueQrCredentialCommand>
{
    public IssueQrCredentialCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("معرف المركز مطلوب.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("معرف الطالب مطلوب.");
    }
}
