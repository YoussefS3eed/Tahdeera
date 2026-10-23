using FluentValidation;

namespace Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceLedger;

public class GetWorkspaceFinanceLedgerValidator : AbstractValidator<GetWorkspaceFinanceLedgerQuery>
{
    public GetWorkspaceFinanceLedgerValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty();

        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate.Value <= x.ToDate.Value)
            .WithMessage("INVALID_DATE_RANGE");
    }
}
