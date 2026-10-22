using FluentValidation;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Tahdeera.Application.Common.Behaviors;

namespace Tahdeera.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 1. FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // 2. Mapster
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        // 3. MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        });

        // 4. Financial Services
        services.AddScoped<Tahdeera.Application.Features.Finances.Services.IPaymentAllocationEngine, Tahdeera.Application.Features.Finances.Services.PaymentAllocationEngine>();

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services.AddApplicationServices();
}
