namespace Tahdeera.Application.Features.Students.DTOs;

public record StudentAuditLogItemDto(
    string Action,
    DateTimeOffset OccurredAt,
    string ActorName
);
