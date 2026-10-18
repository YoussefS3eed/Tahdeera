using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Subscriptions.Commands.UpdateSubscriptionPlan;

public record UpdateSubscriptionPlanCommand(
    Guid Id,
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
    bool IsActive) : IRequest<Result<SubscriptionPlanDto>>, ITransactionalRequest;
