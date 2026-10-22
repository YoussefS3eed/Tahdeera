using MediatR;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Finances.Queries.GetStudentCharges;

public record GetStudentChargesQuery(
    Guid WorkspaceId,
    Guid StudentId,
    ChargeStatus? Status = null,
    string? BillingPeriod = null) : IRequest<List<StudentChargeDto>>;
