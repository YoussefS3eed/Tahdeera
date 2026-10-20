using FluentValidation;

namespace Tahdeera.Application.Features.Sessions.Commands.CancelSession;

public sealed class CancelSessionCommandValidator : AbstractValidator<CancelSessionCommand>
{
    public CancelSessionCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required.");

        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");

        RuleFor(x => x.ActorUserId)
            .NotEmpty().WithMessage("Actor User ID is required.");

        RuleFor(x => x.CancellationReason)
            .NotEmpty()
            .WithErrorCode("CANCELLATION_REASON_REQUIRED")
            .WithMessage("Cancellation reason is required.");
    }
}
