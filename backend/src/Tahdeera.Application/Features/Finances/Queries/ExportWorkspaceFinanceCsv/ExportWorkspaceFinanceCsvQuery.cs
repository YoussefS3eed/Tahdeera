using MediatR;

namespace Tahdeera.Application.Features.Finances.Queries.ExportWorkspaceFinanceCsv;

public record ExportWorkspaceFinanceCsvQuery(
    Guid WorkspaceId,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    Guid? GroupId = null,
    Guid? CashierUserId = null,
    string? PaymentMethod = null,
    string? TransactionType = null
) : IRequest<IAsyncEnumerable<string>>;
