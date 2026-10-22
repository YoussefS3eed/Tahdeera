using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Finances.Commands.RecordStudentPayment;

public record RecordStudentPaymentCommand(
    Guid WorkspaceId,
    Guid StudentId,
    decimal Amount,
    PaymentMethod Method,
    DateTimeOffset? PaidAt,
    string? Reference,
    string? Notes,
    IReadOnlyList<PaymentAllocationRequestDto>? Allocations) : IRequest<RecordPaymentResultDto>, ITransactionalRequest;
