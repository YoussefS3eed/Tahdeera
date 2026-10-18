using FluentValidation;

namespace Tahdeera.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithErrorCode("REFRESH_TOKEN_REQUIRED")
            .WithMessage("Refresh token is required.");
    }
}
