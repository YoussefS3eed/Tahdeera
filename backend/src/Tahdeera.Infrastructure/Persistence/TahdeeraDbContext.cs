using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence;

public class TahdeeraDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>, ITahdeeraDbContext
{
    public TahdeeraDbContext(DbContextOptions<TahdeeraDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<WorkspaceSubscription> WorkspaceSubscriptions => Set<WorkspaceSubscription>();
    public DbSet<SubscriptionPayment> SubscriptionPayments => Set<SubscriptionPayment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentAcademicHistory> StudentAcademicHistories => Set<StudentAcademicHistory>();
    public DbSet<WorkspaceStudent> WorkspaceStudents => Set<WorkspaceStudent>();
    public DbSet<StudentCredential> StudentCredentials => Set<StudentCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TahdeeraDbContext).Assembly);
    }
}
