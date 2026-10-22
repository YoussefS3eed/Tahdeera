using MediatR;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.Application.Features.Finances.Commands.StageTuitionReminders;

public record StageTuitionRemindersCommand(
    Guid WorkspaceId,
    Guid? GroupId,
    decimal MinimumBalanceThreshold
) : IRequest<StageTuitionRemindersResultDto>;
