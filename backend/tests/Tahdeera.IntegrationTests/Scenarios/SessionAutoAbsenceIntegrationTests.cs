using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tahdeera.Application.Features.Sessions.Commands.CompleteSession;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class SessionAutoAbsenceIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public SessionAutoAbsenceIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CompleteSession_GeneratesSystemAbsences_ExecutesDisciplinarySuspension()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var admin = await db.AppUsers.FirstAsync(u => u.IsPlatformAdmin);
        var workspace = new Workspace { Name = "AutoAbsence_WS", Kind = "Center", CreatedByUserId = admin.Id };
        var academicYear = new AcademicYear { Name = "2026/2027", StartsOn = new DateOnly(2026, 9, 1), EndsOn = new DateOnly(2027, 6, 30), IsCurrent = true };
        var grade = await db.Grades.OrderBy(g => g.SortOrder).FirstAsync();

        await db.Workspaces.AddAsync(workspace);
        await db.AcademicYears.AddAsync(academicYear);
        await db.SaveChangesAsync();

        var teacher = new AppUser { Id = Guid.NewGuid(), Username = "absence_teacher", DisplayName = "Absence Teacher", NormalizedEmail = "ABSENCE_TEACHER@TEST.COM", Phone = "+201011115555", IsActive = true };
        await db.AppUsers.AddAsync(teacher);

        // Group with threshold of 1 consecutive absence -> MarkSuspended
        var group = Group.Create(workspace.Id, academicYear.Id, grade.Id, "Absence Policy Cohort", teacher.Id, consecutiveAbsenceThreshold: 1, absenceAction: AbsenceAction.MarkSuspended);
        await db.Groups.AddAsync(group);
        await db.SaveChangesAsync();

        // Student 1: Attended
        var student1 = new Student { Id = Guid.NewGuid(), FullName = "Attending Student", StudentCode = "STU-00005" };
        await db.Students.AddAsync(student1);
        var wsStudent1 = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            StudentId = student1.Id,
            
            Status = WorkspaceStudentStatus.Active
        };
        await db.WorkspaceStudents.AddAsync(wsStudent1);
        var enrollment1 = GroupStudent.Create(group.Id, student1.Id, DateTimeOffset.UtcNow);
        await db.GroupStudents.AddAsync(enrollment1);

        // Student 2: Unattended (Absent) -> Should get suspended
        var student2 = new Student { Id = Guid.NewGuid(), FullName = "Unattended Student", StudentCode = "STU-00006" };
        await db.Students.AddAsync(student2);
        var wsStudent2 = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            StudentId = student2.Id,
            
            Status = WorkspaceStudentStatus.Active
        };
        await db.WorkspaceStudents.AddAsync(wsStudent2);
        var enrollment2 = GroupStudent.Create(group.Id, student2.Id, DateTimeOffset.UtcNow);
        await db.GroupStudents.AddAsync(enrollment2);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-2),
            EndsAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            LateGraceMinutes = 20,
            Status = SessionStatus.Scheduled
        };
        await db.Sessions.AddAsync(session);

        // Record attendance for Student 1 only
        var attendance1 = AttendanceRecord.CreateCheckIn(session, student1.Id, workspace.Id, DateTimeOffset.UtcNow.AddHours(-1), teacher.Id);
        await db.AttendanceRecords.AddAsync(attendance1);
        await db.SaveChangesAsync();

        // Act: Complete session
        using var opScope = _factory.Services.CreateScope();
        var mediator = opScope.ServiceProvider.GetRequiredService<IMediator>();

        var completeResult = await mediator.Send(new CompleteSessionCommand(
            WorkspaceId: workspace.Id,
            SessionId: session.Id,
            ActorUserId: teacher.Id
        ));

        // Assert: Succeeded
        completeResult.IsSuccess.Should().BeTrue();
        completeResult.Value.GeneratedAbsencesCount.Should().Be(1);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        // 1. Session is completed
        var reloadedSession = await verifyDb.Sessions.FirstAsync(s => s.Id == session.Id);
        reloadedSession.Status.Should().Be(SessionStatus.Completed);

        // 2. Student 2 has system-generated Absent record
        var absentRecord2 = await verifyDb.AttendanceRecords
            .FirstOrDefaultAsync(r => r.SessionId == session.Id && r.StudentId == student2.Id);
        absentRecord2.Should().NotBeNull();
        absentRecord2!.Status.Should().Be(AttendanceStatus.Absent);
        absentRecord2.Source.Should().Be(AttendanceSource.System);

        // 3. Student 2 was suspended due to reaching consecutive absence threshold
        var reloadedEnrollment2 = await verifyDb.GroupStudents.FirstAsync(gs => gs.Id == enrollment2.Id);
        reloadedEnrollment2.Status.Should().Be(GroupStudentStatus.Suspended);

        // 4. Student 1 remains Active
        var reloadedEnrollment1 = await verifyDb.GroupStudents.FirstAsync(gs => gs.Id == enrollment1.Id);
        reloadedEnrollment1.Status.Should().Be(GroupStudentStatus.Active);
    }
}
