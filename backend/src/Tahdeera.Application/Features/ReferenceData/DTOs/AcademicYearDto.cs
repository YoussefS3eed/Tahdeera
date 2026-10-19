namespace Tahdeera.Application.Features.ReferenceData.DTOs;

public record AcademicYearDto(
    Guid Id,
    string NameAr,
    string NameEn,
    DateOnly StartsOn,
    DateOnly EndsOn,
    bool IsCurrent
);
