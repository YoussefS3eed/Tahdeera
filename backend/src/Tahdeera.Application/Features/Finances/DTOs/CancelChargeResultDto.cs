namespace Tahdeera.Application.Features.Finances.DTOs;

public record CancelChargeResultDto(
    Guid ChargeId,
    string Status,
    string CancellationReason,
    DateTimeOffset CancelledAt,
    int VoidedAllocationsCount,
    decimal FreedCreditAmount);
