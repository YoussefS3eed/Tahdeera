using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tahdeera.Application.Features.Attendance.Commands.RegisterGuestMakeupAttendance;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class MakeupAttendanceIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public MakeupAttendanceIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GuestMakeupAttendance_IsolatesRoster_CreatesZeroInvoices_ExcusesHomeSession()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var admin = await db.AppUsers.FirstAsync(u => u.IsPlatformAdmin);
        var workspace = new Workspace { Name = "Makeup_WS", Kind = "Center", CreatedByUserId = admin.Id };
        var academicYear = new AcademicYear { Name = "2026/2027", StartsOn = new DateOnly(2026, 9, 1), EndsOn = new DateOnly(2027, 6, 30), IsCurrent = true };
        var grade = await db.Grades.OrderBy(g => g.SortOrder).FirstAsync();

        await db.Workspaces.AddAsync(workspace);
        await db.AcademicYears.AddAsync(academicYear);
        await db.SaveChangesAsync();

        var teacher = new AppUser { Id = Guid.NewGuid(), Username = "makeup_teacher", DisplayName = "Makeup Teacher", NormalizedEmail = "MAKEUP_TEACHER@TEST.COM", Phone = "+201011117777", IsActive = true };
        await db.AppUsers.AddAsync(teacher);

        // Home group & Visited group
        var homeGroup = Group.Create(workspace.Id, academicYear.Id, grade.Id, "Home Group Saturday", teacher.Id);
        var visitedGroup = Group.Create(workspace.Id, academicYear.Id, grade.Id, "Visited Group Tuesday", teacher.Id);
        await db.Groups.AddRangeAsync(homeGroup, visitedGroup);
        await db.SaveChangesAsync();

        // Student enrolled in Home Group
        var student = new Student { Id = Guid.NewGuid(), FullName = "Guest Makeup Student", StudentCode = "STU-00003" };
        await db.Students.AddAsync(student);

        var wsStudent = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            StudentId = student.Id,
            
            Status = WorkspaceStudentStatus.Active
        };
        await db.WorkspaceStudents.AddAsync(wsStudent);

        var acadHist = new StudentAcademicHistory
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            AcademicYearId = academicYear.Id,
            GradeId = grade.Id
        };
        await db.StudentAcademicHistories.AddAsync(acadHist);

        var homeEnrollment = GroupStudent.Create(homeGroup.Id, student.Id, DateTimeOffset.UtcNow);
        await db.GroupStudents.AddAsync(homeEnrollment);

        // Past session in Home group where student was Absent
        var homeSession = new Session
        {
            Id = Guid.NewGuid(),
            GroupId = homeGroup.Id,
            StartsAt = DateTimeOffset.UtcNow.AddDays(-2),
            EndsAt = DateTimeOffset.UtcNow.AddDays(-2).AddHours(2),
            LateGraceMinutes = 20,
            Status = SessionStatus.Completed
        };
        await db.Sessions.AddAsync(homeSession);

        var homeAbsentRecord = AttendanceRecord.CreateSystemAbsence(homeSession.Id, student.Id, workspace.Id, DateTimeOffset.UtcNow.AddDays(-2).AddHours(2));
        await db.AttendanceRecords.AddAsync(homeAbsentRecord);

        // Active session in Visited group
        var visitedSession = new Session
        {
            Id = Guid.NewGuid(),
            GroupId = visitedGroup.Id,
            StartsAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            EndsAt = DateTimeOffset.UtcNow.AddHours(2),
            LateGraceMinutes = 20,
            Status = SessionStatus.Scheduled
        };
        await db.Sessions.AddAsync(visitedSession);
        await db.SaveChangesAsync();

        // Act: Register guest make-up attendance in visited session
        using var opScope = _factory.Services.CreateScope();
        var mediator = opScope.ServiceProvider.GetRequiredService<IMediator>();

        var makeupResult = await mediator.Send(new RegisterGuestMakeupAttendanceCommand(
            SessionId: visitedSession.Id,
            StudentId: student.Id,
            HomeGroupId: homeGroup.Id,
            HomeSessionId: homeSession.Id,
            Reason: "Attending Tuesday group to cover missed Saturday class"
        ));

        // Assert: Command succeeded
        makeupResult.IsSuccess.Should().BeTrue();
        makeupResult.Value.IsGuestMakeup.Should().BeTrue();
        makeupResult.Value.BillingStatus.Should().Be("CoveredByHomeSubscription");
        makeupResult.Value.GuestInvoicesCreated.Should().Be(0);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        // 1. Visited group roster in group_students must NOT contain student
        var visitedMemberships = await verifyDb.GroupStudents.Where(gs => gs.GroupId == visitedGroup.Id).ToListAsync();
        visitedMemberships.Should().BeEmpty();

        // 2. Attendance record in visited session is marked guest make-up
        var visitedRecord = await verifyDb.AttendanceRecords.FirstAsync(r => r.SessionId == visitedSession.Id);
        visitedRecord.IsGuestMakeup.Should().BeTrue();
        visitedRecord.HomeGroupId.Should().Be(homeGroup.Id);

        // 3. Home session absence was automatically excused per FR-26
        var reloadedHomeRecord = await verifyDb.AttendanceRecords.FirstAsync(r => r.Id == homeAbsentRecord.Id);
        reloadedHomeRecord.Status.Should().Be(AttendanceStatus.Excused);
    }
}
