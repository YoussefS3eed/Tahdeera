namespace Tahdeera.Application.Features.Students.DTOs;

public record StudentDetailsDto(
    Guid StudentId,
    string StudentCode,
    string FullName,
    short GradeId,
    string GradeNameAr,
    string GradeNameEn,
    string Status,
    string? GuardianName,
    string? GuardianPhone,
    string? StudentPhone,
    bool WhatsappOptIn,
    string? SeatNumber,
    string? LocalNotes,
    bool HasActiveCard,
    DateTimeOffset EnrolledAt
);
