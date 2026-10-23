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

        // 3. Defensive Security Headers
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Append("X-XSS-Protection", "0");
            context.Response.Headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
            context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none';");
            await next();
        });

        // 4. Swagger UI at root (Development only to prevent production information disclosure)
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tahdeera API v1");
                c.RoutePrefix = string.Empty;
            });
        }

        // 5. CORS & Transport Hardening
        app.UseCors();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        // 6. Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // 7. Rate Limiting
        app.UseRateLimiter();

        // 8. Server-Side Subscription Lockout Middleware (DEC-08)
        app.UseMiddleware<SubscriptionLockoutMiddleware>();

        // 9. Health Checks (Constitution Principle 10)
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        // 10. Controllers
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
