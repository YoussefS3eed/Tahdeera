namespace Tahdeera.Application.Features.Students.DTOs;

public record WorkspaceEnrollmentDetailsDto(
    string Status,
    string? GuardianName,
    string? GuardianPhone,
    string? StudentPhone,
    bool WhatsappOptIn,
    string? SeatNumber,
    string? LocalNotes,
    DateTimeOffset EnrolledAt
);
