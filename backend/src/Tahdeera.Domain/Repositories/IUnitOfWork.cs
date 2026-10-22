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

    // Milestone B3 Repositories
    IGroupRepository Groups { get; }
    IGenericRepository<GroupStudent> GroupStudents { get; }
    IGenericRepository<GroupStudentStatusHistory> GroupStudentStatusHistories { get; }
    IGenericRepository<GroupAttendanceDelegation> GroupAttendanceDelegations { get; }
    IGenericRepository<GroupWorkspaceAccess> GroupWorkspaceAccesses { get; }
    ISessionRepository Sessions { get; }
    IAttendanceRecordRepository AttendanceRecords { get; }
    IGenericRepository<AttendanceHistory> AttendanceHistories { get; }
    IGenericRepository<MessageOutbox> MessageOutboxes { get; }

    // Milestone B4 WhatsApp & Outbox Repositories
    IMessageOutboxRepository MessageOutbox { get; }
    IWorkspaceWhatsAppConfigRepository WorkspaceWhatsAppConfigs { get; }
    IWhatsAppSenderConnectionRepository WhatsAppSenderConnections { get; }
    IWorkspaceWhatsAppUsageLedgerRepository WorkspaceWhatsAppUsageLedgers { get; }

    // Milestone B5 Student Tuition Finance Repositories
    IStudentAccountRepository StudentAccounts { get; }
    IStudentChargeRepository StudentCharges { get; }
    IStudentPaymentRepository StudentPayments { get; }
    IPaymentAllocationRepository PaymentAllocations { get; }
    IWorkspaceReceiptSequenceRepository WorkspaceReceiptSequences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
