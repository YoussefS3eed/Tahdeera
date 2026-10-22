namespace Tahdeera.Application.Features.Finances.DTOs;

public record WriteOffChargeResultDto(
    Guid ChargeId,
    string Status,
    string WriteOffReason,
    DateTimeOffset WrittenOffAt,
    int VoidedAllocationsCount,
    decimal FreedCreditAmount);
