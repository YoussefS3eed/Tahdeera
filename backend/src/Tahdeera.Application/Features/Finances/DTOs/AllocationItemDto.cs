namespace Tahdeera.Application.Features.Finances.DTOs;

public record AllocationItemDto(
    Guid ChargeId,
    string ChargeDescription,
    decimal AllocatedAmount);
