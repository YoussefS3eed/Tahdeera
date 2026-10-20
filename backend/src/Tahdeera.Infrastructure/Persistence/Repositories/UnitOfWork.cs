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

    // Milestone B3 domain repositories
    private IGroupRepository? _groups;
    private ISessionRepository? _sessions;
    private IAttendanceRecordRepository? _attendanceRecords;
    private IGenericRepository<GroupStudent>? _groupStudents;
    private IGenericRepository<GroupStudentStatusHistory>? _groupStudentStatusHistories;
    private IGenericRepository<GroupAttendanceDelegation>? _groupAttendanceDelegations;
    private IGenericRepository<GroupWorkspaceAccess>? _groupWorkspaceAccesses;
    private IGenericRepository<AttendanceHistory>? _attendanceHistories;
    private IGenericRepository<MessageOutbox>? _messageOutboxes;

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

    // Milestone B3 domain repositories
    public IGroupRepository Groups =>
        _groups ??= new GroupRepository(_context);

    public ISessionRepository Sessions =>
        _sessions ??= new SessionRepository(_context);

    public IAttendanceRecordRepository AttendanceRecords =>
        _attendanceRecords ??= new AttendanceRecordRepository(_context);

    public IGenericRepository<GroupStudent> GroupStudents =>
        _groupStudents ??= new GenericRepository<GroupStudent>(_context);

    public IGenericRepository<GroupStudentStatusHistory> GroupStudentStatusHistories =>
        _groupStudentStatusHistories ??= new GenericRepository<GroupStudentStatusHistory>(_context);

    public IGenericRepository<GroupAttendanceDelegation> GroupAttendanceDelegations =>
        _groupAttendanceDelegations ??= new GenericRepository<GroupAttendanceDelegation>(_context);

    public IGenericRepository<GroupWorkspaceAccess> GroupWorkspaceAccesses =>
        _groupWorkspaceAccesses ??= new GenericRepository<GroupWorkspaceAccess>(_context);

    public IGenericRepository<AttendanceHistory> AttendanceHistories =>
        _attendanceHistories ??= new GenericRepository<AttendanceHistory>(_context);

    public IGenericRepository<MessageOutbox> MessageOutboxes =>
        _messageOutboxes ??= new GenericRepository<MessageOutbox>(_context);

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
