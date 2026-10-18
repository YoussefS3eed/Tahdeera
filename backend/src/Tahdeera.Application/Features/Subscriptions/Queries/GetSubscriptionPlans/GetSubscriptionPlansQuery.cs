using MediatR;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Subscriptions.Queries.GetSubscriptionPlans;

public record GetSubscriptionPlansQuery(bool ActiveOnly = false) : IRequest<Result<List<SubscriptionPlanDto>>>;
