using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IUnitOfWork : IDisposable
{
    ISubscriptionPlanRepository SubscriptionPlans { get; }
    IWorkspaceRepository Workspaces { get; }
    IGenericRepository<AppUser> AppUsers { get; }
    IGenericRepository<WorkspaceMember> WorkspaceMembers { get; }
    IGenericRepository<WorkspaceSubscription> WorkspaceSubscriptions { get; }
    IGenericRepository<SubscriptionPayment> SubscriptionPayments { get; }
    IGenericRepository<AuditLog> AuditLogs { get; }
    IGenericRepository<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
