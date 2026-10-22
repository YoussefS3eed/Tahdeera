namespace Tahdeera.Application.Features.Finances.DTOs;

public record PrintableReceiptDto(
    // Workspace Metadata
    Guid WorkspaceId,
    string WorkspaceName,
    string? WorkspacePhone,
    string? WorkspaceAddress,

    // Student Metadata
    Guid StudentId,
    string StudentCode,
    string StudentName,
    string? SeatNumber,
    string? GradeName,

    // Payment Transaction Data
    Guid PaymentId,
    string ReceiptNumber,
    DateTimeOffset PaidAt,
    string PaymentMethod,
    string? Reference,
    decimal Amount,
    string Currency, // "EGP"
    string AmountInWordsArabic, // Tafqeet: "فقط أربعمائة وخمسون جنيهاً مصرياً لا غير"
    string CashierName,

    // Charge Settlements
    IReadOnlyList<ReceiptSettlementItemDto> Settlements,

    // Balance Snapshot
    decimal RemainingAccountBalance,
    string BalanceStatus, // "Settled", "InArrears", "Overpaid"

    // Anti-Counterfeit Verification
    string VerificationHash, // HMAC-SHA256
    string VerificationPath  // "/verify-receipt/{hash}"
);

public record ReceiptSettlementItemDto(
    Guid ChargeId,
    string Description,
    string? BillingPeriodLabel,
    decimal AllocatedAmount,
    decimal RemainingChargeBalance
);
