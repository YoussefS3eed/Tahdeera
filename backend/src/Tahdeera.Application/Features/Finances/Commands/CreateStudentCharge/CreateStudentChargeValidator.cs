using FluentValidation;

namespace Tahdeera.Application.Features.Finances.Commands.CreateStudentCharge;

public class CreateStudentChargeValidator : AbstractValidator<CreateStudentChargeCommand>
{
    public CreateStudentChargeValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("AMOUNT_MUST_BE_POSITIVE");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("CHARGE_DESCRIPTION_REQUIRED")
            .MinimumLength(3).WithMessage("CHARGE_DESCRIPTION_REQUIRED")
            .MaximumLength(240).WithMessage("CHARGE_DESCRIPTION_REQUIRED");

        RuleFor(x => x.BillingPeriodLabel)
            .MaximumLength(60)
            .When(x => !string.IsNullOrEmpty(x.BillingPeriodLabel));
    }
}
