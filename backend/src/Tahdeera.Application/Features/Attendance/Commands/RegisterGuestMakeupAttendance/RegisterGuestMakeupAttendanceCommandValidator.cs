using FluentValidation;

namespace Tahdeera.Application.Features.Attendance.Commands.RegisterGuestMakeupAttendance;

public sealed class RegisterGuestMakeupAttendanceCommandValidator : AbstractValidator<RegisterGuestMakeupAttendanceCommand>
{
    public RegisterGuestMakeupAttendanceCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty();

        RuleFor(x => x.StudentId)
            .NotEmpty();

        RuleFor(x => x.HomeGroupId)
            .NotEmpty();

        RuleFor(x => x.Reason)
            .MaximumLength(500);
    }
}
