using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;

public record CreateSubscriptionPlanCommand(
    string Code,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    decimal DefaultPrice,
    string Currency = "EGP",
    int DurationDays = 365,
    int MaxStudents = 150,
    int MaxStaff = 1,
    int MaxBranches = 1,
    SubscriptionPlanFeaturesDto? Features = null) : IRequest<Result<SubscriptionPlanDto>>, ITransactionalRequest;
