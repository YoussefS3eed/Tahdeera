using FluentValidation;

namespace Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;

public class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workspace name is required.")
            .MaximumLength(200).WithMessage("Workspace name cannot exceed 200 characters.");

        RuleFor(x => x.Kind)
            .NotEmpty().WithMessage("Workspace kind is required.")
            .Must(k => k == "Teacher" || k == "Center")
            .WithMessage("Workspace kind must be either 'Teacher' or 'Center'.");

        When(x => !string.IsNullOrEmpty(x.ContactPhone), () =>
        {
            RuleFor(x => x.ContactPhone!)
                .MaximumLength(11).WithMessage("Contact phone cannot exceed 11 digits.");
        });

        When(x => x.DefaultLateGraceMinutes.HasValue, () =>
        {
            RuleFor(x => x.DefaultLateGraceMinutes!.Value)
                .InclusiveBetween(0, 1440).WithMessage("Default late grace minutes must be between 0 and 1440.");
        });

        RuleFor(x => x.Owner).NotNull().WithMessage("Owner information is required.");
        When(x => x.Owner != null, () =>
        {
            RuleFor(x => x.Owner.Username)
                .NotEmpty().WithMessage("Owner username is required.")
                .MaximumLength(100).WithMessage("Owner username cannot exceed 100 characters.");

            RuleFor(x => x.Owner.Email)
                .NotEmpty().WithMessage("Owner email is required.")
                .EmailAddress().WithMessage("Owner email is not valid.")
                .MaximumLength(320).WithMessage("Owner email cannot exceed 320 characters.");

            RuleFor(x => x.Owner.Password)
                .NotEmpty().WithMessage("Owner password is required.")
                .MinimumLength(6).WithMessage("Owner password must be at least 6 characters.");

            RuleFor(x => x.Owner.DisplayName)
                .NotEmpty().WithMessage("Owner display name is required.")
                .MaximumLength(200).WithMessage("Owner display name cannot exceed 200 characters.");

            When(x => !string.IsNullOrEmpty(x.Owner.Phone), () =>
            {
                RuleFor(x => x.Owner.Phone!)
                    .MaximumLength(11).WithMessage("Phone number cannot exceed 11 digits.");
            });
        });

        RuleFor(x => x.Subscription).NotNull().WithMessage("Subscription details are required.");
        When(x => x.Subscription != null, () =>
        {
            RuleFor(x => x.Subscription.PlanId)
                .NotEmpty().WithMessage("Subscription plan ID is required.");

            RuleFor(x => x.Subscription.EndsAt)
                .GreaterThan(x => x.Subscription.StartsAt)
                .WithMessage("Subscription end date must be after start date.");

            RuleFor(x => x.Subscription.AgreedPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Agreed price must be non-negative.");

            RuleFor(x => x.Subscription.InitialPayment).NotNull().WithMessage("Initial payment details are required.");
            When(x => x.Subscription.InitialPayment != null, () =>
            {
                RuleFor(x => x.Subscription.InitialPayment.Amount)
                    .GreaterThan(0).WithMessage("Initial payment amount must be greater than 0.");

                RuleFor(x => x.Subscription.InitialPayment.Method)
                    .NotEmpty().WithMessage("Payment method is required.");
            });
        });
    }
}
