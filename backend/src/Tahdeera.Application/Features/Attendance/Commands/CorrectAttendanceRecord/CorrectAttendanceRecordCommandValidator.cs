using FluentValidation;

namespace Tahdeera.Application.Features.Attendance.Commands.CorrectAttendanceRecord;

public sealed class CorrectAttendanceRecordCommandValidator : AbstractValidator<CorrectAttendanceRecordCommand>
{
    public CorrectAttendanceRecordCommandValidator()
    {
        RuleFor(x => x.AttendanceRecordId)
            .NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.NewStatus)
            .IsInEnum();
    }
}
