using FluentValidation;

namespace Tahdeera.Application.Features.Workspaces.Commands.AddWorkspaceMember;

public class AddWorkspaceMemberCommandValidator : AbstractValidator<AddWorkspaceMemberCommand>
{
    private static readonly string[] AllowedRoles = ["Teacher", "Assistant", "Reception", "Accountant"];

    public AddWorkspaceMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("معرف مساحة العمل مطلوب.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("اسم المستخدم مطلوب.")
            .Length(3, 100).WithMessage("يجب أن يكون اسم المستخدم بين 3 و 100 حرف.")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("اسم المستخدم يمكن أن يحتوي على أحرف وأرقام وشرطة سفلية فقط.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("البريد الإلكتروني مطلوب.")
            .MaximumLength(320).WithMessage("البريد الإلكتروني لا يجب أن يتجاوز 320 حرفاً.")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة.")
            .MinimumLength(8).WithMessage("يجب ألا تقل كلمة المرور عن 8 أحرف.")
            .Matches(@"[A-Z]").WithMessage("يجب أن تحتوي كلمة المرور على حرف كبير واحد على الأقل.")
            .Matches(@"[0-9]").WithMessage("يجب أن تحتوي كلمة المرور على رقم واحد على الأقل.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("يجب أن تحتوي كلمة المرور على رمز خاص واحد على الأقل.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("الاسم الظاهر مطلوب.")
            .Length(2, 200).WithMessage("يجب أن يكون الاسم الظاهر بين 2 و 200 حرف.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("الدور مطلوب.")
            .Must(role => AllowedRoles.Contains(role))
            .WithMessage("الدور غير صالح. الأدوار المسموح بها: Teacher, Assistant, Reception, Accountant.");
    }
}
