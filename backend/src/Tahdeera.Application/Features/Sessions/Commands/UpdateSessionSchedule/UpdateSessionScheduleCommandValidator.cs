using FluentValidation;

namespace Tahdeera.Application.Features.Sessions.Commands.UpdateSessionSchedule;

public sealed class UpdateSessionScheduleCommandValidator : AbstractValidator<UpdateSessionScheduleCommand>
{
    public UpdateSessionScheduleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required.");

        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");

        RuleFor(x => x.ActorUserId)
            .NotEmpty().WithMessage("Actor User ID is required.");

        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithErrorCode("SESSION_INVALID_TIME_RANGE")
            .WithMessage("Session end time must be strictly after start time.");

        RuleFor(x => x.LateGraceMinutes)
            .InclusiveBetween(0, 1440)
            .WithMessage("Late grace minutes must be between 0 and 1440.");
    }
}
