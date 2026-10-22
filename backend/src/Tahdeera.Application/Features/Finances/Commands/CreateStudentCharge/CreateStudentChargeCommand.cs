using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.Application.Features.Finances.Commands.CreateStudentCharge;

public record CreateStudentChargeCommand(
    Guid WorkspaceId,
    Guid StudentId,
    Guid? GroupId,
    string? BillingPeriodLabel,
    string Description,
    decimal Amount,
    DateTimeOffset? DueAt,
    string? Notes) : IRequest<StudentChargeDto>, ITransactionalRequest;
