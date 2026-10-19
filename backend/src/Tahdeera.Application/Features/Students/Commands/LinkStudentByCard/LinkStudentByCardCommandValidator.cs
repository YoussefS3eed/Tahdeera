using FluentValidation;

namespace Tahdeera.Application.Features.Students.Commands.LinkStudentByCard;

public class LinkStudentByCardCommandValidator : AbstractValidator<LinkStudentByCardCommand>
{
    public LinkStudentByCardCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("WorkspaceId is required.");

        RuleFor(x => x.ScannedToken)
            .NotEmpty()
            .WithMessage("ScannedToken is required.")
            .MaximumLength(500)
            .WithMessage("ScannedToken cannot exceed 500 characters.");

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

        When(x => x.GradeId.HasValue, () =>
        {
            RuleFor(x => x.GradeId!.Value)
                .InclusiveBetween((short)1, (short)12)
                .WithMessage("GradeId must be between 1 and 12.");
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
    }
}
