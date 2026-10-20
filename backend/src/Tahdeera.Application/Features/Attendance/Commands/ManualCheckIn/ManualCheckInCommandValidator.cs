using FluentValidation;

namespace Tahdeera.Application.Features.Attendance.Commands.ManualCheckIn;

public sealed class ManualCheckInCommandValidator : AbstractValidator<ManualCheckInCommand>
{
    public ManualCheckInCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty();

        RuleFor(x => x.StudentId)
            .NotEmpty();

        RuleFor(x => x.RetroactiveReason)
            .MaximumLength(500);
    }
}
