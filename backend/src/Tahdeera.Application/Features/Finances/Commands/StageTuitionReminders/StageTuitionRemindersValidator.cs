using FluentValidation;

namespace Tahdeera.Application.Features.Finances.Commands.StageTuitionReminders;

public class StageTuitionRemindersValidator : AbstractValidator<StageTuitionRemindersCommand>
{
    public StageTuitionRemindersValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.MinimumBalanceThreshold).GreaterThan(0).WithMessage("AMOUNT_MUST_BE_POSITIVE");
    }
}
