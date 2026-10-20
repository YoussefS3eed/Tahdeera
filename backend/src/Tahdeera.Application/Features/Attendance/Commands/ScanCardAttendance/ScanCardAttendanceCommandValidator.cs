using FluentValidation;

namespace Tahdeera.Application.Features.Attendance.Commands.ScanCardAttendance;

public sealed class ScanCardAttendanceCommandValidator : AbstractValidator<ScanCardAttendanceCommand>
{
    public ScanCardAttendanceCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty();

        RuleFor(x => x.RawToken)
            .NotEmpty()
            .MaximumLength(500);
    }
}
