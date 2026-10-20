using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Attendance.Queries.GetSessionAttendanceSheet;
using Tahdeera.Application.Features.Attendance.Queries.GetStudentAttendanceHistory;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Attendance;

public class SessionAttendanceSheetTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_GetSessionAttendanceSheet_ShouldReturnAccurateRosterAndCounts()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);

        var session = Session.Create(group.Id, baseTime, baseTime.AddHours(2), 20, _actorUserId);
        context.Sessions.Add(session);

        var student1 = new Student { Id = Guid.NewGuid(), StudentCode = "STU_SHEET_1", FullName = "Hassan" };
        var student2 = new Student { Id = Guid.NewGuid(), StudentCode = "STU_SHEET_2", FullName = "Mohamed" };
        var groupStudent1 = GroupStudent.Create(group.Id, student1.Id, baseTime.AddDays(-10));
        var groupStudent2 = GroupStudent.Create(group.Id, student2.Id, baseTime.AddDays(-10));

        var record1 = AttendanceRecord.CreateCheckIn(session, student1.Id, _workspaceId, baseTime.AddMinutes(5), _actorUserId);
        var record2 = AttendanceRecord.CreateSystemAbsence(session.Id, student2.Id, _workspaceId, baseTime.AddHours(2));

        context.Students.AddRange(student1, student2);
        context.GroupStudents.AddRange(groupStudent1, groupStudent2);
        context.AttendanceRecords.AddRange(record1, record2);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);

        var handler = new GetSessionAttendanceSheetQueryHandler(unitOfWork, currentUserService);
        var query = new GetSessionAttendanceSheetQuery(session.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEnrolled.Should().Be(2);
        result.Value.PresentCount.Should().Be(1);
        result.Value.AbsentCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_GetStudentAttendanceHistory_ShouldReturnCorrectPercentageAndTimeline()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);

        var session1 = Session.Create(group.Id, baseTime.AddDays(-7), baseTime.AddDays(-7).AddHours(2), 20, _actorUserId);
        var session2 = Session.Create(group.Id, baseTime.AddDays(-3), baseTime.AddDays(-3).AddHours(2), 20, _actorUserId);
        context.Sessions.AddRange(session1, session2);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_HIST_1", FullName = "Samy" };
        var groupStudent = GroupStudent.Create(group.Id, student.Id, baseTime.AddDays(-15));

        // Session 1: Present, Session 2: Absent
        var record1 = AttendanceRecord.CreateCheckIn(session1, student.Id, _workspaceId, session1.StartsAt.AddMinutes(5), _actorUserId);
        var record2 = AttendanceRecord.CreateSystemAbsence(session2.Id, student.Id, _workspaceId, session2.EndsAt);

        context.Students.Add(student);
        context.GroupStudents.Add(groupStudent);
        context.AttendanceRecords.AddRange(record1, record2);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);

        var handler = new GetStudentAttendanceHistoryQueryHandler(unitOfWork, currentUserService);
        var query = new GetStudentAttendanceHistoryQuery(student.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSessions.Should().Be(2);
        result.Value.AttendedCount.Should().Be(1);
        result.Value.AbsentCount.Should().Be(1);
        result.Value.AttendancePercentage.Should().Be(50.0);
        result.Value.Timeline.Should().HaveCount(2);
    }
}
