namespace Tahdeera.Application.Features.Finances.DTOs;

public record BatchCreateGroupChargesResultDto(
    Guid GroupId,
    string GroupName,
    string BillingPeriodLabel,
    int TotalActiveEnrolled,
    int ChargesCreatedCount,
    int SkippedDuplicatesCount,
    int AutoSettledFromCreditCount,
    decimal TotalBilledAmount,
    IReadOnlyList<Guid> CreatedChargeIds
);
