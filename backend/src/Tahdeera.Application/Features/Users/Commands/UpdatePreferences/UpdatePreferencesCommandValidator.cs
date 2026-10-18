using FluentValidation;

namespace Tahdeera.Application.Features.Users.Commands.UpdatePreferences;

public class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.PreferredLanguage) || !string.IsNullOrWhiteSpace(x.PreferredTheme))
            .WithErrorCode("AT_LEAST_ONE_PREFERENCE_REQUIRED")
            .WithMessage("At least one preference (language or theme) must be provided.");

        When(x => !string.IsNullOrWhiteSpace(x.PreferredLanguage), () =>
        {
            RuleFor(x => x.PreferredLanguage!)
                .Must(l => l == "ar" || l == "en")
                .WithErrorCode("INVALID_LANGUAGE")
                .WithMessage("Preferred language must be either 'ar' or 'en'.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.PreferredTheme), () =>
        {
            RuleFor(x => x.PreferredTheme!)
                .Must(t => t == "light" || t == "dark")
                .WithErrorCode("INVALID_THEME")
                .WithMessage("Preferred theme must be 'light' or 'dark'.");
        });
    }
}
