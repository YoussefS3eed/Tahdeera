using FluentValidation;

namespace Tahdeera.Application.Features.Finances.Commands.RecordStudentPayment;

public class RecordStudentPaymentValidator : AbstractValidator<RecordStudentPaymentCommand>
{
    public RecordStudentPaymentValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("AMOUNT_MUST_BE_POSITIVE");

        RuleFor(x => x.Method)
            .IsInEnum()
            .WithMessage("INVALID_PAYMENT_METHOD");

        RuleFor(x => x.PaidAt)
            .Must(date => date == null || date <= DateTimeOffset.UtcNow.AddMinutes(5))
            .WithMessage("PAYMENT_DATE_CANNOT_BE_FUTURE");

        RuleFor(x => x.Reference)
            .MaximumLength(160)
            .When(x => !string.IsNullOrEmpty(x.Reference));

        RuleFor(x => x)
            .Must(cmd => cmd.Allocations == null || cmd.Allocations.Count == 0 || cmd.Allocations.Sum(a => a.Amount) <= cmd.Amount)
            .WithMessage("ALLOCATION_EXCEEDS_PAYMENT")
            .When(x => x.Allocations != null && x.Allocations.Count > 0);
    }
}
