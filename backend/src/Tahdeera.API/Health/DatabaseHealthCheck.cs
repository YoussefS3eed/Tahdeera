using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tahdeera.Infrastructure.Persistence;

namespace Tahdeera.API.Health;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly TahdeeraDbContext _context;

    public DatabaseHealthCheck(TahdeeraDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL is connected and responding.")
                : HealthCheckResult.Unhealthy("Cannot connect to PostgreSQL database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL health check encountered an exception.", ex);
        }
    }
}
