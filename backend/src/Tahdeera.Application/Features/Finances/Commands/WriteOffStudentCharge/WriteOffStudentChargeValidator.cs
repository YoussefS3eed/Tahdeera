using FluentValidation;

namespace Tahdeera.Application.Features.Finances.Commands.WriteOffStudentCharge;

public class WriteOffStudentChargeValidator : AbstractValidator<WriteOffStudentChargeCommand>
{
    public WriteOffStudentChargeValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(5)
            .WithMessage("CANCELLATION_REASON_TOO_SHORT");
    }
}
