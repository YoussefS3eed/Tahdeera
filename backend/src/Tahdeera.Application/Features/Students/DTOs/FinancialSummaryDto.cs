namespace Tahdeera.Application.Features.Students.DTOs;

public record FinancialSummaryDto(
    decimal TotalBilled,
    decimal TotalPaid,
    decimal OutstandingBalance
);
