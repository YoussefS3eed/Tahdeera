using FluentValidation;

namespace Tahdeera.Application.Features.Finances.Commands.BatchCreateGroupCharges;

public class BatchCreateGroupChargesValidator : AbstractValidator<BatchCreateGroupChargesCommand>
{
    public BatchCreateGroupChargesValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty().WithMessage("GROUP_NOT_FOUND_IN_WORKSPACE");
        RuleFor(x => x.BillingPeriodLabel).NotEmpty().MaximumLength(60).WithMessage("BILLING_PERIOD_REQUIRED");
        RuleFor(x => x.Description).NotEmpty().MinimumLength(3).MaximumLength(240).WithMessage("CHARGE_DESCRIPTION_REQUIRED");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("AMOUNT_MUST_BE_POSITIVE");
    }
}
