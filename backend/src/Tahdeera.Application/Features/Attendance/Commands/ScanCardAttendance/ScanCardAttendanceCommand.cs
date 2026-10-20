using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Attendance.Commands.ScanCardAttendance;

public sealed record ScanCardAttendanceCommand(
    Guid SessionId,
    string RawToken
) : IRequest<Result<AttendanceScanResultDto>>, ITransactionalRequest;
