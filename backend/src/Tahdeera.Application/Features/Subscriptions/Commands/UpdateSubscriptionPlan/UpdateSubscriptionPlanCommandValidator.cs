using FluentValidation;

namespace Tahdeera.Application.Features.Subscriptions.Commands.UpdateSubscriptionPlan;

public class UpdateSubscriptionPlanCommandValidator : AbstractValidator<UpdateSubscriptionPlanCommand>
{
    public UpdateSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Plan ID is required.");

        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("Arabic name is required.")
            .MaximumLength(120).WithMessage("Arabic name cannot exceed 120 characters.");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("English name is required.")
            .MaximumLength(120).WithMessage("English name cannot exceed 120 characters.");

        RuleFor(x => x.DefaultPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Default price must be non-negative.");

        RuleFor(x => x.MaxStudents)
            .GreaterThan(0).WithMessage("Max students must be greater than 0.");

        RuleFor(x => x.MaxStaff)
            .GreaterThan(0).WithMessage("Max staff must be greater than 0.");

        RuleFor(x => x.MaxBranches)
            .GreaterThan(0).WithMessage("Max branches must be greater than 0.");

        RuleFor(x => x.DurationDays)
            .GreaterThan(0).WithMessage("Duration in days must be greater than 0.");
    }
}
