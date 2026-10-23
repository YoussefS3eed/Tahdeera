using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Attendance.Commands.CorrectAttendanceRecord;

public sealed record CorrectAttendanceRecordCommand(
    Guid AttendanceRecordId,
    AttendanceStatus NewStatus,
    string Reason,
    bool IsOwner = false,
    Guid WorkspaceId = default
) : IRequest<Result<AttendanceCorrectedDto>>, ITransactionalRequest;
