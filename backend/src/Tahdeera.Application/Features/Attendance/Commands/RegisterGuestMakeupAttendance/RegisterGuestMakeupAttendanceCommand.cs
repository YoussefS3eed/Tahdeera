using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Attendance.Commands.RegisterGuestMakeupAttendance;

public sealed record RegisterGuestMakeupAttendanceCommand(
    Guid SessionId,
    Guid StudentId,
    Guid HomeGroupId,
    Guid? HomeSessionId = null,
    string? Reason = null,
    Guid WorkspaceId = default
) : IRequest<Result<GuestMakeupResultDto>>, ITransactionalRequest;
