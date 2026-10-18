using FluentValidation;

namespace Tahdeera.Application.Features.Subscriptions.Commands.RenewWorkspaceSubscription;

public class RenewWorkspaceSubscriptionCommandValidator : AbstractValidator<RenewWorkspaceSubscriptionCommand>
{
    private static readonly string[] AllowedMethods = ["Cash", "BankTransfer", "Wallet", "Card", "Other"];

    public RenewWorkspaceSubscriptionCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Renewal amount must be greater than zero.");

        RuleFor(x => x.Method)
            .NotEmpty().WithMessage("Payment method is required.")
            .Must(m => AllowedMethods.Contains(m))
            .WithMessage($"Payment method must be one of: {string.Join(", ", AllowedMethods)}.");

        RuleFor(x => x.DurationDays)
            .GreaterThan(0).WithMessage("Duration in days must be greater than zero.");

        RuleFor(x => x)
            .Must(x => !x.OverrideStartsAt.HasValue || !x.OverrideEndsAt.HasValue || x.OverrideEndsAt.Value > x.OverrideStartsAt.Value)
            .WithMessage("Override ends date must be strictly after override starts date.");
    }
}
