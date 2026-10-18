using Mapster;
using Tahdeera.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.Commands.UpdateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.ValueObjects;

namespace Tahdeera.Application.Common.Mappings;

public class MappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // 1. SubscriptionPlanFeatures <-> SubscriptionPlanFeaturesDto
        config.NewConfig<SubscriptionPlanFeatures, SubscriptionPlanFeaturesDto>();
        config.NewConfig<SubscriptionPlanFeaturesDto, SubscriptionPlanFeatures>();

        // 2. SubscriptionPlan -> SubscriptionPlanDto
        config.NewConfig<SubscriptionPlan, SubscriptionPlanDto>()
            .Map(dest => dest.Features, src => src.Features.Adapt<SubscriptionPlanFeaturesDto>());

        // 3. CreateSubscriptionPlanCommand -> SubscriptionPlan
        config.NewConfig<CreateSubscriptionPlanCommand, SubscriptionPlan>()
            .Map(dest => dest.Code, src => src.Code.Trim().ToUpperInvariant())
            .Map(dest => dest.NameAr, src => src.NameAr.Trim())
            .Map(dest => dest.NameEn, src => src.NameEn.Trim())
            .Map(dest => dest.DescriptionAr, src => src.DescriptionAr != null ? src.DescriptionAr.Trim() : null)
            .Map(dest => dest.DescriptionEn, src => src.DescriptionEn != null ? src.DescriptionEn.Trim() : null)
            .Map(dest => dest.Currency, src => string.IsNullOrWhiteSpace(src.Currency) ? "EGP" : src.Currency.Trim().ToUpperInvariant())
            .Map(dest => dest.Features, src => (src.Features ?? new SubscriptionPlanFeaturesDto()).Adapt<SubscriptionPlanFeatures>())
            .Map(dest => dest.IsActive, _ => true);

        // 4. UpdateSubscriptionPlanCommand -> SubscriptionPlan
        config.NewConfig<UpdateSubscriptionPlanCommand, SubscriptionPlan>()
            .Map(dest => dest.NameAr, src => src.NameAr.Trim())
            .Map(dest => dest.NameEn, src => src.NameEn.Trim())
            .Map(dest => dest.DescriptionAr, src => src.DescriptionAr != null ? src.DescriptionAr.Trim() : null)
            .Map(dest => dest.DescriptionEn, src => src.DescriptionEn != null ? src.DescriptionEn.Trim() : null)
            .Map(dest => dest.Currency, src => string.IsNullOrWhiteSpace(src.Currency) ? "EGP" : src.Currency.Trim().ToUpperInvariant())
            .Map(dest => dest.Features, src => src.Features.Adapt<SubscriptionPlanFeatures>())
            .Map(dest => dest.IsActive, src => src.IsActive);
    }
}
