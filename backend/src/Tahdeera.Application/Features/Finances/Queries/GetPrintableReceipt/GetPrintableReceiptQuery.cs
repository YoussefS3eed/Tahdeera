using MediatR;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.Application.Features.Finances.Queries.GetPrintableReceipt;

public record GetPrintableReceiptQuery(
    Guid WorkspaceId,
    Guid StudentId,
    Guid PaymentId
) : IRequest<PrintableReceiptDto>;
