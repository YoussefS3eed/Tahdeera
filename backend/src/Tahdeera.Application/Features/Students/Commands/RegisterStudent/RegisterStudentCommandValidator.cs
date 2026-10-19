using FluentValidation;

namespace Tahdeera.Application.Features.Students.Commands.RegisterStudent;

public class RegisterStudentCommandValidator : AbstractValidator<RegisterStudentCommand>
{
    public RegisterStudentCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("WorkspaceId is required.");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Student full name is required.")
            .Length(2, 200)
            .WithMessage("Student full name must be between 2 and 200 characters.");

        RuleFor(x => x.GradeId)
            .InclusiveBetween((short)1, (short)12)
            .WithMessage("GradeId must be between 1 and 12.");

        RuleFor(x => x.GuardianPhone)
            .NotEmpty()
            .WithMessage("Guardian phone is required.")
            .Matches(@"^(?:\+201|01)[0-2,5]\d{8}$")
            .WithMessage("Guardian phone must be a valid Egyptian mobile number.");

        When(x => !string.IsNullOrWhiteSpace(x.StudentPhone), () =>
        {
            RuleFor(x => x.StudentPhone!)
                .Matches(@"^(?:\+201|01)[0-2,5]\d{8}$")
                .WithMessage("Student phone must be a valid Egyptian mobile number.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.SeatNumber), () =>
        {
            RuleFor(x => x.SeatNumber!)
                .MaximumLength(50)
                .WithMessage("Seat number cannot exceed 50 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.LocalNotes), () =>
        {
            RuleFor(x => x.LocalNotes!)
                .MaximumLength(2000)
                .WithMessage("Local notes cannot exceed 2000 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.CardToken), () =>
        {
            RuleFor(x => x.CardToken!)
                .MaximumLength(500)
                .WithMessage("Card token cannot exceed 500 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.CardUidDisplay), () =>
        {
            RuleFor(x => x.CardUidDisplay!)
                .MaximumLength(64)
                .WithMessage("Card UID display cannot exceed 64 characters.");
        });
    }
}
