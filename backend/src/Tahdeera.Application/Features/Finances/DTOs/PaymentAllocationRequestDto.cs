namespace Tahdeera.Application.Features.Finances.DTOs;

public record PaymentAllocationRequestDto(
    Guid ChargeId,
    decimal Amount);
