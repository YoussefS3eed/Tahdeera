using MediatR;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.Application.Features.Finances.Queries.GetStudentPayments;

public record GetStudentPaymentsQuery(
    Guid WorkspaceId,
    Guid StudentId) : IRequest<List<StudentPaymentDto>>;
