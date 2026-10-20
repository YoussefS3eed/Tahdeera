using FluentValidation;

namespace Tahdeera.Application.Features.Groups.Commands.EnrollStudentInGroup;

public sealed class EnrollStudentInGroupCommandValidator : AbstractValidator<EnrollStudentInGroupCommand>
{
    public EnrollStudentInGroupCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required.");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.ActorUserId)
            .NotEmpty().WithMessage("Actor User ID is required.");
    }
}
