using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tahdeera.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Tahdeera.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory managing an ephemeral PostgreSQL 18 container via Testcontainers.
/// Configures dynamic connection string overrides, applies EF Core migrations,
/// seeds initial plans and super admin, and provides database reset routines.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("tahdeera_test")
        .WithUsername("postgres")
        .WithPassword("TestPassword123!")
        .Build();

    public PostgreSqlContainer DbContainer => _dbContainer;

    public async Task InitializeAsync()
    {
        // 1. Start ephemeral PostgreSQL container
        await _dbContainer.StartAsync();

        // 2. Trigger web host creation (which runs SeedDatabaseAsync with MigrateAsync)
        _ = CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set dynamic connection string in configuration
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", "SuperSecretJwtSigningKeyWith256BitsMinimumLengthForDevelopmentOnly!");
        builder.UseSetting("Jwt:Issuer", "https://api.tahdeera.app");
        builder.UseSetting("Jwt:Audience", "https://tahdeera.app");

        builder.ConfigureServices(services =>
        {
            // Re-configure TahdeeraDbContext to use dynamic Testcontainer connection string
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TahdeeraDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<TahdeeraDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString())
                       .UseSnakeCaseNamingConvention()
                       .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });
        });
    }

    /// <summary>
    /// Resets mutable database state between test classes, preserving seed plans and Super Admin.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        await DatabaseResetHelper.ResetAsync(context, userManager);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }
}
