using Microsoft.Extensions.DependencyInjection;

namespace Tahdeera.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register Application layer services (e.g. MediatR, FluentValidation, Mappers)
        return services;
    }
}
