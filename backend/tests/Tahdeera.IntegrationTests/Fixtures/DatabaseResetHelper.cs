using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.Infrastructure.Persistence.Seed;

namespace Tahdeera.IntegrationTests.Fixtures;

/// <summary>
/// Fast database truncation helper.
/// Truncates mutable domain tables with CASCADE to preserve foreign key integrity,
/// deletes non-admin identity accounts, and re-invokes DatabaseSeeder to ensure
/// the baseline Super Admin account and subscription plans are permanently intact.
/// </summary>
public static class DatabaseResetHelper
{
    public static async Task ResetAsync(TahdeeraDbContext context, UserManager<IdentityUser<Guid>> userManager)
    {
        // 1. Truncate mutable workspace, member, token, and payment tables
        await context.Database.ExecuteSqlRawAsync(@"
            TRUNCATE TABLE 
                audit_logs, 
                subscription_payments, 
                workspace_subscriptions, 
                workspace_members, 
                refresh_tokens, 
                workspaces 
            CASCADE;
        ");

        // 2. Remove all non-admin users created during tests
        await context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM app_users WHERE username != 'admin';
            DELETE FROM ""AspNetUsers"" WHERE user_name != 'admin';
        ");

        // 3. Ensure baseline subscription plans and Super Admin exist
        await DatabaseSeeder.SeedAsync(context, userManager);
    }
}
