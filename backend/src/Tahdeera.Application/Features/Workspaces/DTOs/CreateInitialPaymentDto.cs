namespace Tahdeera.Application.Features.Workspaces.DTOs;

public record CreateInitialPaymentDto(
    decimal Amount,
    string Method,
    string? Reference,
    string? Notes);
