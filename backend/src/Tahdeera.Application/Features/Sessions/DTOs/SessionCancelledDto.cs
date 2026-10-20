namespace Tahdeera.Application.Features.Sessions.DTOs;

public sealed record SessionCancelledDto(
    Guid SessionId,
    DateTimeOffset CancelledAt,
    string CancellationReason,
    int VoidedAttendanceRecordsCount);
