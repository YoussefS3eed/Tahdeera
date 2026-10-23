using FluentValidation;

namespace Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceStudents;

public class GetWorkspaceFinanceStudentsValidator : AbstractValidator<GetWorkspaceFinanceStudentsQuery>
{
    private static readonly string[] AllowedStatuses = ["all", "paid", "partial", "unpaid", "settled"];

    public GetWorkspaceFinanceStudentsValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty();

        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PAGE_SIZE_EXCEEDED");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || AllowedStatuses.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage("INVALID_FINANCE_STATUS_FILTER");
    }
}
