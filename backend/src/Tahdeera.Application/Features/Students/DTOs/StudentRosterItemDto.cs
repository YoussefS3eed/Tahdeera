namespace Tahdeera.Application.Features.Students.DTOs;

public record StudentRosterItemDto(
    Guid StudentId,
    string StudentCode,
    string FullName,
    short GradeId,
    string GradeNameAr,
    string GradeNameEn,
    string Status,
    string? GuardianPhone,
    string? StudentPhone,
    bool WhatsappOptIn,
    string? SeatNumber,
    bool HasActiveCard,
    DateTimeOffset EnrolledAt
);
