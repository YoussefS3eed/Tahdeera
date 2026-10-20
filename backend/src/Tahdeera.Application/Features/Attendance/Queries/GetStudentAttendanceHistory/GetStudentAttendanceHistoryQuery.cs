using MediatR;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Attendance.Queries.GetStudentAttendanceHistory;

public sealed record GetStudentAttendanceHistoryQuery(
    Guid StudentId,
    Guid? GroupId = null
) : IRequest<Result<StudentAttendanceHistoryDto>>;
