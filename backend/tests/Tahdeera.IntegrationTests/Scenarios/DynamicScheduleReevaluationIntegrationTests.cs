using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tahdeera.Application.Features.Sessions.Commands.UpdateSessionSchedule;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class DynamicScheduleReevaluationIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public DynamicScheduleReevaluationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DelayingSessionStartTime_ReevaluatesLateToPresent_PreservesLockedRecords()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var admin = await db.AppUsers.FirstAsync(u => u.IsPlatformAdmin);
        var workspace = new Workspace { Name = "DynamicReeval_WS", Kind = "Center", CreatedByUserId = admin.Id };
        var academicYear = new AcademicYear { Name = "2026/2027", StartsOn = new DateOnly(2026, 9, 1), EndsOn = new DateOnly(2027, 6, 30), IsCurrent = true };
        var grade = await db.Grades.OrderBy(g => g.SortOrder).FirstAsync();

        await db.Workspaces.AddAsync(workspace);
        await db.AcademicYears.AddAsync(academicYear);
        await db.SaveChangesAsync();

        var teacher = new AppUser { Id = Guid.NewGuid(), Username = "reeval_teacher", DisplayName = "Reeval Teacher", NormalizedEmail = "REEVAL_TEACHER@TEST.COM", Phone = "+201011118888", IsActive = true };
        await db.AppUsers.AddAsync(teacher);

        var group = Group.Create(workspace.Id, academicYear.Id, grade.Id, "Reeval Physics Cohort", teacher.Id);
        await db.Groups.AddAsync(group);
        await db.SaveChangesAsync();

        var baseTime = DateTimeOffset.UtcNow;
        var originalStart = baseTime.AddHours(1);
        var originalEnd = originalStart.AddHours(2);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            StartsAt = originalStart,
            EndsAt = originalEnd,
            LateGraceMinutes = 10,
            Status = SessionStatus.Scheduled
        };
        await db.Sessions.AddAsync(session);

        // Student 1: Checked in at start + 15 min -> Currently Late, Unlocked
        var student1 = new Student { Id = Guid.NewGuid(), FullName = "Late Student 1", StudentCode = "STU-00001" };
        await db.Students.AddAsync(student1);
        var record1 = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            StudentId = student1.Id,
            OwnerWorkspaceId = workspace.Id,
            Status = AttendanceStatus.Late,
            Source = AttendanceSource.User,
            CheckInAt = originalStart.AddMinutes(15),
            RecordedByUserId = teacher.Id,
            RecordedAt = originalStart.AddMinutes(15),
            LastModifiedAt = originalStart.AddMinutes(15),
            AutoEvaluationLocked = false
        };
        await db.AttendanceRecords.AddAsync(record1);

        // Student 2: Manually marked Late, AutoEvaluationLocked = true
        var student2 = new Student { Id = Guid.NewGuid(), FullName = "Locked Student 2", StudentCode = "STU-00002" };
        await db.Students.AddAsync(student2);
        var record2 = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            StudentId = student2.Id,
            OwnerWorkspaceId = workspace.Id,
            Status = AttendanceStatus.Late,
            Source = AttendanceSource.User,
            CheckInAt = originalStart.AddMinutes(15),
            RecordedByUserId = teacher.Id,
            RecordedAt = originalStart.AddMinutes(15),
            LastModifiedAt = originalStart.AddMinutes(15),
            AutoEvaluationLocked = true
        };
        await db.AttendanceRecords.AddAsync(record2);

        await db.SaveChangesAsync();

        // Act: Delay session start time forward by 15 minutes (so check-in at originalStart + 15 min becomes start + 0 min <= grace of 10 min -> Present)
        var newStart = originalStart.AddMinutes(15);
        var newEnd = originalEnd.AddMinutes(15);

        using var opScope = _factory.Services.CreateScope();
        var mediator = opScope.ServiceProvider.GetRequiredService<IMediator>();
        var updateResult = await mediator.Send(new UpdateSessionScheduleCommand(
            WorkspaceId: workspace.Id,
            SessionId: session.Id,
            StartsAt: newStart,
            EndsAt: newEnd,
            LateGraceMinutes: 10,
            ActorUserId: teacher.Id
        ));

        // Assert
        updateResult.IsSuccess.Should().BeTrue();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var reloadedRecord1 = await verifyDb.AttendanceRecords.FirstAsync(r => r.StudentId == student1.Id);
        reloadedRecord1.Status.Should().Be(AttendanceStatus.Present);

        var reloadedRecord2 = await verifyDb.AttendanceRecords.FirstAsync(r => r.StudentId == student2.Id);
        reloadedRecord2.Status.Should().Be(AttendanceStatus.Late);
        reloadedRecord2.AutoEvaluationLocked.Should().BeTrue();

        var reevalHistory = await verifyDb.AttendanceHistories
            .Where(h => h.AttendanceRecordId == record1.Id && h.ChangeKind == AttendanceChangeKind.SystemReevaluation)
            .ToListAsync();
        reevalHistory.Should().NotBeEmpty();
    }
}
