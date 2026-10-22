using FluentValidation;

namespace Tahdeera.Application.Features.Finances.Commands.ReverseStudentPayment;

public class ReverseStudentPaymentValidator : AbstractValidator<ReverseStudentPaymentCommand>
{
    public ReverseStudentPaymentValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("REVERSAL_REASON_TOO_SHORT")
            .MinimumLength(5).WithMessage("REVERSAL_REASON_TOO_SHORT");
    }
}
