namespace Tahdeera.Application.Features.Students.DTOs;

public record AcademicHistoryRecordDto(
    Guid AcademicYearId,
    string AcademicYearName,
    short GradeId,
    string GradeNameAr,
    string GradeNameEn,
    bool IsCurrentYear,
    DateTimeOffset RecordedAt
);
