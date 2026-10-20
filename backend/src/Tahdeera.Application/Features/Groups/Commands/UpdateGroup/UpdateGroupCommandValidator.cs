using FluentValidation;

namespace Tahdeera.Application.Features.Groups.Commands.UpdateGroup;

public sealed class UpdateGroupCommandValidator : AbstractValidator<UpdateGroupCommand>
{
    public UpdateGroupCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required.");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MaximumLength(160).WithMessage("Group name cannot exceed 160 characters.");

        RuleFor(x => x.LateGraceMinutes)
            .InclusiveBetween(0, 1440)
            .WithMessage("Late grace minutes must be between 0 and 1440.");

        RuleFor(x => x.DefaultMonthlyFee)
            .GreaterThanOrEqualTo(0.00m)
            .WithMessage("Default monthly fee cannot be negative.");

        RuleFor(x => x.ConsecutiveAbsenceThreshold)
            .InclusiveBetween((short)1, (short)50)
            .When(x => x.ConsecutiveAbsenceThreshold.HasValue)
            .WithErrorCode("INVALID_ABSENCE_THRESHOLD")
            .WithMessage("Consecutive absence threshold must be between 1 and 50.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThan(0)
            .When(x => x.MaxCapacity.HasValue)
            .WithMessage("Max capacity must be greater than 0.");
    }
}
