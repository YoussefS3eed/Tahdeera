using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Application.Common.Interfaces;

public interface ITahdeeraDbContext
{
    DbSet<AppUser> AppUsers { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Workspace> Workspaces { get; }
    DbSet<WorkspaceMember> WorkspaceMembers { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<WorkspaceSubscription> WorkspaceSubscriptions { get; }
    DbSet<SubscriptionPayment> SubscriptionPayments { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
