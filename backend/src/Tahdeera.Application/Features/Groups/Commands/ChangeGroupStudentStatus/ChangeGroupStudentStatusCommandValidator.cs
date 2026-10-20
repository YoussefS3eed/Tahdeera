using FluentValidation;

namespace Tahdeera.Application.Features.Groups.Commands.ChangeGroupStudentStatus;

public sealed class ChangeGroupStudentStatusCommandValidator : AbstractValidator<ChangeGroupStudentStatusCommand>
{
    public ChangeGroupStudentStatusCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required.");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.ActorUserId)
            .NotEmpty().WithMessage("Actor User ID is required.");

        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Invalid student group status.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithErrorCode("CORRECTION_REASON_REQUIRED")
            .WithMessage("Correction reason is required.");
    }
}
