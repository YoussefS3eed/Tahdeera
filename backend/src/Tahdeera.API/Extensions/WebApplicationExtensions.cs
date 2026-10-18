using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tahdeera.API.Middlewares;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.Infrastructure.Persistence.Seed;

namespace Tahdeera.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // 1. Request Localization (ar default, en supported)
        var supportedCultures = new[] { "ar", "en" };
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture("ar")
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);
        app.UseRequestLocalization(localizationOptions);

        // 2. Global RFC 9457 Exception Handling Middleware
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // 3. Swagger UI at root
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tahdeera API v1");
            c.RoutePrefix = string.Empty;
        });


        // 4. CORS
        app.UseCors();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // 5. Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // 6. Rate Limiting
        app.UseRateLimiter();

        // 7. Server-Side Subscription Lockout Middleware (DEC-08)
        app.UseMiddleware<SubscriptionLockoutMiddleware>();

        // 7. Health Checks (Constitution Principle 10)
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        // 8. Controllers
        app.MapControllers();

        return app;
    }

    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        await context.Database.MigrateAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        await DatabaseSeeder.SeedAsync(context, userManager);
    }
}
