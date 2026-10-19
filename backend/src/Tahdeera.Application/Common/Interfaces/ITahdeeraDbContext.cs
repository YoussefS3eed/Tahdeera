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

    DbSet<Grade> Grades { get; }
    DbSet<AcademicYear> AcademicYears { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<Student> Students { get; }
    DbSet<StudentAcademicHistory> StudentAcademicHistories { get; }
    DbSet<WorkspaceStudent> WorkspaceStudents { get; }
    DbSet<StudentCredential> StudentCredentials { get; }

    DbSet<Group> Groups { get; }
    DbSet<GroupStudent> GroupStudents { get; }
    DbSet<GroupStudentStatusHistory> GroupStudentStatusHistories { get; }
    DbSet<GroupWorkspaceAccess> GroupWorkspaceAccesses { get; }
    DbSet<GroupAttendanceDelegation> GroupAttendanceDelegations { get; }
    DbSet<Session> Sessions { get; }
    DbSet<AttendanceRecord> AttendanceRecords { get; }
    DbSet<AttendanceHistory> AttendanceHistories { get; }
    DbSet<MessageOutbox> MessageOutboxes { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
