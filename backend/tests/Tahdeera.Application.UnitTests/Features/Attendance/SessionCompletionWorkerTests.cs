using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.BackgroundJobs;

namespace Tahdeera.Application.UnitTests.Features.Attendance;

public class SessionCompletionWorkerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task ProcessExpiredSessionsAsync_WhenPastBuffer_ShouldMarkCompletedAndGenerateAbsences()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var context = TestDbContextFactory.Create(dbName);
        var baseTime = DateTimeOffset.UtcNow;

        var workspace = new Workspace
        {
            Id = _workspaceId,
            Name = "Al-Amal Center",
            AutoAbsenceBufferMinutes = 15
        };
        context.Workspaces.Add(workspace);

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Biology A", _actorUserId);
        context.Groups.Add(group);

        // Session ended 30 minutes ago (past the 15-minute buffer)
        var session = Session.Create(group.Id, baseTime.AddHours(-2), baseTime.AddMinutes(-30), 20, _actorUserId);
        context.Sessions.Add(session);

        // Student 1 attended, Student 2 unattended
        var student1 = new Student { Id = Guid.NewGuid(), StudentCode = "STU_ATT_1", FullName = "Hany" };
        var student2 = new Student { Id = Guid.NewGuid(), StudentCode = "STU_ATT_2", FullName = "Samir" };
        var gs1 = GroupStudent.Create(group.Id, student1.Id, baseTime.AddDays(-10));
        var gs2 = GroupStudent.Create(group.Id, student2.Id, baseTime.AddDays(-10));

        var ws2 = new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student2.Id,
            Status = WorkspaceStudentStatus.Active,
            WhatsappOptIn = true,
            GuardianPhone = "+201088776655"
        };

        // Attendance record for student 1
        var record1 = AttendanceRecord.CreateCheckIn(session, student1.Id, _workspaceId, session.StartsAt, _actorUserId);

        context.Students.AddRange(student1, student2);
        context.GroupStudents.AddRange(gs1, gs2);
        context.WorkspaceStudents.Add(ws2);
        context.AttendanceRecords.Add(record1);
        await context.SaveChangesAsync();

        // Setup DI scope factory with distinct context instance per scope
        var services = new ServiceCollection();
        services.AddScoped(_ => TestDbContextFactory.Create(dbName));
        var mockReconciliation = Substitute.For<IAttendanceReconciliationService>();
        services.AddScoped(_ => mockReconciliation);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new AttendanceWorkerOptions { SessionCompletionIntervalMinutes = 10, BatchSize = 50 });
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime);
        var logger = Substitute.For<ILogger<SessionCompletionWorker>>();

        var worker = new SessionCompletionWorker(scopeFactory, options, timeProvider, logger);

        // Act
        var processedCount = await worker.ProcessExpiredSessionsAsync(CancellationToken.None);

        // Assert
        processedCount.Should().Be(1);

        using var assertContext = TestDbContextFactory.Create(dbName);
        var sessionInDb = await assertContext.Sessions.FirstAsync(s => s.Id == session.Id);
        sessionInDb.Status.Should().Be(SessionStatus.Completed);
        sessionInDb.AutoAbsenceProcessedAt.Should().NotBeNull();

        // Student 2 received automated absence
        var absenceInDb = await assertContext.AttendanceRecords.FirstOrDefaultAsync(r => r.SessionId == session.Id && r.StudentId == student2.Id);
        absenceInDb.Should().NotBeNull();
        absenceInDb!.Status.Should().Be(AttendanceStatus.Absent);
        absenceInDb.Source.Should().Be(AttendanceSource.System);
        absenceInDb.AutoEvaluationLocked.Should().BeFalse();

        // Outbox notification staged for student 2
        var outboxInDb = await assertContext.MessageOutboxes.FirstOrDefaultAsync(m => m.StudentId == student2.Id);
        outboxInDb.Should().NotBeNull();
        outboxInDb!.RecipientPhone.Should().Be("+201088776655");
        outboxInDb.IdempotencyKey.Should().Be($"att_{absenceInDb.Id}_Absent");
    }

    [Fact]
    public async Task ProcessExpiredSessionsAsync_WhenWithinBuffer_ShouldNotProcessSession()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var context = TestDbContextFactory.Create(dbName);
        var baseTime = DateTimeOffset.UtcNow;

        var workspace = new Workspace
        {
            Id = _workspaceId,
            Name = "Al-Amal Center",
            AutoAbsenceBufferMinutes = 15
        };
        context.Workspaces.Add(workspace);

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Biology A", _actorUserId);
        context.Groups.Add(group);

        // Session ended only 5 minutes ago (within 15-minute buffer)
        var session = Session.Create(group.Id, baseTime.AddHours(-1), baseTime.AddMinutes(-5), 20, _actorUserId);
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddScoped(_ => TestDbContextFactory.Create(dbName));
        var mockReconciliation = Substitute.For<IAttendanceReconciliationService>();
        services.AddScoped(_ => mockReconciliation);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new AttendanceWorkerOptions { SessionCompletionIntervalMinutes = 10, BatchSize = 50 });
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime);
        var logger = Substitute.For<ILogger<SessionCompletionWorker>>();

        var worker = new SessionCompletionWorker(scopeFactory, options, timeProvider, logger);

        // Act
        var processedCount = await worker.ProcessExpiredSessionsAsync(CancellationToken.None);

        // Assert
        processedCount.Should().Be(0);

        using var assertContext = TestDbContextFactory.Create(dbName);
        var sessionInDb = await assertContext.Sessions.FirstAsync(s => s.Id == session.Id);
        sessionInDb.Status.Should().Be(SessionStatus.Scheduled);
    }
}
