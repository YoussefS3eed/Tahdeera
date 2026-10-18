using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly TahdeeraDbContext _context;

    private ISubscriptionPlanRepository? _subscriptionPlans;
    private IWorkspaceRepository? _workspaces;
    private IGenericRepository<AppUser>? _appUsers;
    private IGenericRepository<WorkspaceMember>? _workspaceMembers;
    private IGenericRepository<WorkspaceSubscription>? _workspaceSubscriptions;
    private IGenericRepository<SubscriptionPayment>? _subscriptionPayments;
    private IGenericRepository<AuditLog>? _auditLogs;

    public UnitOfWork(TahdeeraDbContext context)
    {
        _context = context;
    }

    public ISubscriptionPlanRepository SubscriptionPlans =>
        _subscriptionPlans ??= new SubscriptionPlanRepository(_context);

    public IWorkspaceRepository Workspaces =>
        _workspaces ??= new WorkspaceRepository(_context);

    public IGenericRepository<AppUser> AppUsers =>
        _appUsers ??= new GenericRepository<AppUser>(_context);

    public IGenericRepository<WorkspaceMember> WorkspaceMembers =>
        _workspaceMembers ??= new GenericRepository<WorkspaceMember>(_context);

    public IGenericRepository<WorkspaceSubscription> WorkspaceSubscriptions =>
        _workspaceSubscriptions ??= new GenericRepository<WorkspaceSubscription>(_context);

    public IGenericRepository<SubscriptionPayment> SubscriptionPayments =>
        _subscriptionPayments ??= new GenericRepository<SubscriptionPayment>(_context);

    public IGenericRepository<AuditLog> AuditLogs =>
        _auditLogs ??= new GenericRepository<AuditLog>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
