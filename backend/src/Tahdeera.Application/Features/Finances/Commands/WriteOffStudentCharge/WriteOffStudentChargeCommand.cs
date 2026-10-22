using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.Application.Features.Finances.Commands.WriteOffStudentCharge;

public record WriteOffStudentChargeCommand(
    Guid WorkspaceId,
    Guid StudentId,
    Guid ChargeId,
    string Reason) : IRequest<WriteOffChargeResultDto>, ITransactionalRequest;
