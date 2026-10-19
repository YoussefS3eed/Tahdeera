namespace Tahdeera.Application.Features.ReferenceData.DTOs;

public record GradeDto(
    short Id,
    string Code,
    string NameAr,
    string NameEn,
    string Stage,
    short SortOrder
);
