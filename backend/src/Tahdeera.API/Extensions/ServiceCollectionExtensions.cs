using Asp.Versioning;
using Tahdeera.Application;
using Tahdeera.Infrastructure;

namespace Tahdeera.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Controllers & ProblemDetails
        services.AddControllers();
        services.AddProblemDetails();

        // 2. CORS
        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
            {
                var origins = configuration.GetSection("AllowedOrigins").Get<string[]>();
                if (origins != null && origins.Length > 0)
                {
                    policy.WithOrigins(origins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
            })
        );

        // 3. API Versioning (Header: X-Api-Version)
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new HeaderApiVersionReader("X-Api-Version"),
                new HeaderApiVersionReader("api-version")
            );
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = false;
        });

        // 4. Endpoints & Swagger Documentation
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "Tahdeera API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = Microsoft.OpenApi.ParameterLocation.Header,
                Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            c.AddSecurityRequirement(_ => new Microsoft.OpenApi.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer"),
                    new List<string>()
                }
            });
        });

        // 5. Localization
        services.AddLocalization();

        // 6. Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("PlatformAdmin", policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("PlatformAdmin") ||
                    context.User.IsInRole("SuperAdmin") ||
                    context.User.HasClaim(c => c.Type == "is_platform_admin" && c.Value.Equals("true", StringComparison.OrdinalIgnoreCase))));
        });

        // 7. Application + Infrastructure layers
        services.AddApplication();
        services.AddInfrastructure(configuration);

        return services;
    }
}
