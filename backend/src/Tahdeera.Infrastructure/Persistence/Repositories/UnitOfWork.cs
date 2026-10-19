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
    private IAuditLogRepository? _auditLogs;
    private IGenericRepository<RefreshToken>? _refreshTokens;

    // Milestone B2 domain repositories
    private IStudentRepository? _students;
    private IWorkspaceStudentRepository? _workspaceStudents;
    private IGenericRepository<StudentAcademicHistory>? _studentAcademicHistories;
    private IStudentCredentialRepository? _studentCredentials;
    private IGradeRepository? _grades;
    private IAcademicYearRepository? _academicYears;

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

    public IAuditLogRepository AuditLogs =>
        _auditLogs ??= new AuditLogRepository(_context);

    public IGenericRepository<RefreshToken> RefreshTokens =>
        _refreshTokens ??= new GenericRepository<RefreshToken>(_context);

    // Milestone B2 domain repositories
    public IStudentRepository Students =>
        _students ??= new StudentRepository(_context);

    public IWorkspaceStudentRepository WorkspaceStudents =>
        _workspaceStudents ??= new WorkspaceStudentRepository(_context);

    public IGenericRepository<StudentAcademicHistory> StudentAcademicHistories =>
        _studentAcademicHistories ??= new GenericRepository<StudentAcademicHistory>(_context);

    public IStudentCredentialRepository StudentCredentials =>
        _studentCredentials ??= new StudentCredentialRepository(_context);

    public IGradeRepository Grades =>
        _grades ??= new GradeRepository(_context);

    public IAcademicYearRepository AcademicYears =>
        _academicYears ??= new AcademicYearRepository(_context);

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
