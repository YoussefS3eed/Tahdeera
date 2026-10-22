namespace Tahdeera.Application.Features.Finances.DTOs;

public record StudentAccountStatementDto(
    Guid AccountId,
    Guid WorkspaceStudentId,
    Guid StudentId,
    string StudentName,
    string? SeatNumber,
    string Currency,
    decimal CurrentBalance,
    string BalanceStatus,
    decimal TotalLifetimeCharges,
    decimal TotalLifetimePayments,
    decimal TotalOpenCharges,
    decimal TotalUnallocatedCredit,
    decimal? PeriodChargesTotal,
    decimal? PeriodPaymentsTotal,
    decimal? PeriodNetChange,
    IReadOnlyList<AccountTransactionDto> Transactions);
