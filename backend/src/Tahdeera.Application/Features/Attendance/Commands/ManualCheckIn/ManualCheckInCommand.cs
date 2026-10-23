using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Attendance.Commands.ManualCheckIn;

public sealed record ManualCheckInCommand(
    Guid SessionId,
    Guid StudentId,
    string? RetroactiveReason = null,
    Guid WorkspaceId = default
) : IRequest<Result<AttendanceScanResultDto>>, ITransactionalRequest;
