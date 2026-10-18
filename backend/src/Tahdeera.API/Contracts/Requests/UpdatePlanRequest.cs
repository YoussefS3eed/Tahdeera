using Tahdeera.Application.Features.Subscriptions.DTOs;

namespace Tahdeera.API.Contracts.Requests;

public record UpdatePlanRequest(
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
    bool IsActive = true);
