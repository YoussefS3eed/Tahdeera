using FluentValidation;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Students.Commands.ChangeStudentStatus;

public class ChangeStudentStatusCommandValidator : AbstractValidator<ChangeStudentStatusCommand>
{
    public ChangeStudentStatusCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("معرف المركز مطلوب.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("معرف الطالب مطلوب.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("حالة الطالب مطلوبة.")
            .IsEnumName(typeof(WorkspaceStudentStatus), caseSensitive: false)
            .WithMessage("حالة الطالب غير صالحة.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("سبب تغيير الحالة يجب ألا يتجاوز 500 حرف.");
    }
}
