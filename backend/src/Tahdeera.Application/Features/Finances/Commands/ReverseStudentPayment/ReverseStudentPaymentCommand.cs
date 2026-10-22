using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.Application.Features.Finances.Commands.ReverseStudentPayment;

public record ReverseStudentPaymentCommand(
    Guid WorkspaceId,
    Guid StudentId,
    Guid PaymentId,
    string Reason) : IRequest<ReversePaymentResultDto>, ITransactionalRequest;
