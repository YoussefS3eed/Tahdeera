using FluentValidation;

namespace Tahdeera.Application.Features.Students.Commands.UpdateStudentContact;

public class UpdateStudentContactCommandValidator : AbstractValidator<UpdateStudentContactCommand>
{
    private const string EgyptianPhoneRegex = @"^(?:\+201|01)[0-2,5]\d{8}$";

    public UpdateStudentContactCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("معرف المركز مطلوب.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("معرف الطالب مطلوب.");

        When(x => !string.IsNullOrWhiteSpace(x.FullName), () =>
        {
            RuleFor(x => x.FullName)
                .Length(2, 200).WithMessage("اسم الطالب يجب أن يكون بين حرفين و200 حرف.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.GuardianPhone), () =>
        {
            RuleFor(x => x.GuardianPhone)
                .Matches(EgyptianPhoneRegex).WithMessage("رقم هاتف ولي الأمر غير صالح. يجب أن يكون رقماً مصرياً يبدأ بـ 01.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.StudentPhone), () =>
        {
            RuleFor(x => x.StudentPhone)
                .Matches(EgyptianPhoneRegex).WithMessage("رقم هاتف الطالب غير صالح. يجب أن يكون رقماً مصرياً يبدأ بـ 01.");
        });

        RuleFor(x => x.SeatNumber)
            .MaximumLength(50).WithMessage("رقم المقعد يجب ألا يتجاوز 50 حرفاً.");

        RuleFor(x => x.LocalNotes)
            .MaximumLength(2000).WithMessage("الملاحظات يجب ألا تتجاوز 2000 حرف.");
    }
}
