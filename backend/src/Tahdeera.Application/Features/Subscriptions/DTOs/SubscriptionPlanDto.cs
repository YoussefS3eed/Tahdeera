namespace Tahdeera.Application.Features.Subscriptions.DTOs;

public record SubscriptionPlanDto(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    decimal DefaultPrice,
    string Currency,
    int DurationDays,
    int MaxStudents,
    int MaxStaff,
    int MaxBranches,
    SubscriptionPlanFeaturesDto Features,
    bool IsActive);
