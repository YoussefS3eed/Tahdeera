namespace Tahdeera.Application.Features.Finances.DTOs;

public record WorkspaceFinanceSummaryDto(
    Guid WorkspaceId,
    decimal TotalBilled,
    decimal TotalCollected,
    decimal TotalOutstandingArrears,
    int ActiveDebtorCount,
    IReadOnlyDictionary<string, decimal> CollectedByMethod);
