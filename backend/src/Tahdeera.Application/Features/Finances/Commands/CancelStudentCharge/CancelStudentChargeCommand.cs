using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.Application.Features.Finances.Commands.CancelStudentCharge;

public record CancelStudentChargeCommand(
    Guid WorkspaceId,
    Guid StudentId,
    Guid ChargeId,
    string Reason) : IRequest<CancelChargeResultDto>, ITransactionalRequest;
