using MediatR;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Attendance.Queries.GetSessionAttendanceSheet;

public sealed record GetSessionAttendanceSheetQuery(
    Guid WorkspaceId,
    Guid SessionId
) : IRequest<Result<SessionAttendanceSheetDto>>
{
    public GetSessionAttendanceSheetQuery(Guid sessionId) : this(Guid.Empty, sessionId) { }
}
