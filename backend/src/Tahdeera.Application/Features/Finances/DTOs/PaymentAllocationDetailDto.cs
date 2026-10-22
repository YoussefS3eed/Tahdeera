namespace Tahdeera.Application.Features.Finances.DTOs;

public record PaymentAllocationDetailDto(
    Guid ChargeId,
    string ChargeDescription,
    decimal Amount,
    bool IsReversed);
