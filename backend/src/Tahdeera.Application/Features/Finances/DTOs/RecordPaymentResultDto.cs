namespace Tahdeera.Application.Features.Finances.DTOs;

public record RecordPaymentResultDto(
    Guid PaymentId,
    string ReceiptNumber,
    decimal Amount,
    string Method,
    DateTimeOffset PaidAt,
    decimal AllocatedAmount,
    decimal UnallocatedCredit,
    string Status,
    bool WhatsAppReceiptStaged);
