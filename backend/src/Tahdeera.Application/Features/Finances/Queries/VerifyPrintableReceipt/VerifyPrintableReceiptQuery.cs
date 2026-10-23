using MediatR;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.Application.Features.Finances.Queries.VerifyPrintableReceipt;

public sealed record VerifyPrintableReceiptQuery(string ReceiptNumber, string Hash) : IRequest<VerifiedReceiptDto>;
