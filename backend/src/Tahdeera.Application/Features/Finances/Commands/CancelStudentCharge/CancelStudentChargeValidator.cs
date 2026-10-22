using FluentValidation;

namespace Tahdeera.Application.Features.Finances.Commands.CancelStudentCharge;

public class CancelStudentChargeValidator : AbstractValidator<CancelStudentChargeCommand>
{
    public CancelStudentChargeValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(5)
            .WithMessage("CANCELLATION_REASON_TOO_SHORT");
    }
}
