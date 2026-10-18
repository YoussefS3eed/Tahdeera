using FluentValidation;

namespace Tahdeera.Application.Features.Auth.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UsernameOrEmailOrPhone)
            .NotEmpty()
            .WithErrorCode("USERNAME_OR_EMAIL_REQUIRED")
            .WithMessage("Username, email, or phone number is required.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithErrorCode("PASSWORD_REQUIRED")
            .WithMessage("Password is required.");
    }
}
