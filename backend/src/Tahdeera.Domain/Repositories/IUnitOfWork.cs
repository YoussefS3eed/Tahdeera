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
    IAuditLogRepository AuditLogs { get; }
    IGenericRepository<RefreshToken> RefreshTokens { get; }

    // Milestone B2 Repositories
    IStudentRepository Students { get; }
    IWorkspaceStudentRepository WorkspaceStudents { get; }
    IGenericRepository<StudentAcademicHistory> StudentAcademicHistories { get; }
    IStudentCredentialRepository StudentCredentials { get; }
    IGradeRepository Grades { get; }
    IAcademicYearRepository AcademicYears { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
