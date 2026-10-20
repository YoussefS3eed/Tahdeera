using FluentValidation;

namespace Tahdeera.Application.Features.Groups.Commands.AssignAttendanceDelegate;

public sealed class AssignAttendanceDelegateCommandValidator : AbstractValidator<AssignAttendanceDelegateCommand>
{
    public AssignAttendanceDelegateCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required.");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");

        RuleFor(x => x.AssistantUserId)
            .NotEmpty().WithMessage("Assistant User ID is required.");

        RuleFor(x => x.TeacherUserId)
            .NotEmpty().WithMessage("Teacher User ID is required.");
    }
}
