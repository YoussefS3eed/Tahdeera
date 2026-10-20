using FluentAssertions;
using Tahdeera.Application.Features.Sessions.Queries.GetGroupSessions;
using Tahdeera.Application.Features.Sessions.Queries.GetSessionDetails;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Sessions;

public class SessionQueryHandlerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task GetGroupSessions_ShouldFilterAndPaginateSessions()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);

        var s1 = Session.Create(group.Id, DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-2).AddHours(2), 20, _actorUserId);
        s1.MarkCompleted(s1.EndsAt);
        var s2 = Session.Create(group.Id, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddHours(2), 20, _actorUserId);
        context.Sessions.AddRange(s1, s2);
        await context.SaveChangesAsync();

        var handler = new GetGroupSessionsQueryHandler(new UnitOfWork(context));
        var query = new GetGroupSessionsQuery(_workspaceId, group.Id, Status: SessionStatus.Scheduled);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Id.Should().Be(s2.Id);
    }

    [Fact]
    public async Task GetSessionDetails_ShouldReturnAttendanceBreakdownAndRosterCount()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId, roomName: "Lab 3");
        context.Groups.Add(group);

        var student1 = new Student { Id = Guid.NewGuid(), FullName = "S1", StudentCode = "STU-01" };
        var student2 = new Student { Id = Guid.NewGuid(), FullName = "S2", StudentCode = "STU-02" };
        var student3 = new Student { Id = Guid.NewGuid(), FullName = "S3", StudentCode = "STU-03" };
        context.Students.AddRange(student1, student2, student3);

        context.GroupStudents.AddRange(
            GroupStudent.Create(group.Id, student1.Id, DateTimeOffset.UtcNow),
            GroupStudent.Create(group.Id, student2.Id, DateTimeOffset.UtcNow),
            GroupStudent.Create(group.Id, student3.Id, DateTimeOffset.UtcNow));

        var session = Session.Create(group.Id, DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow, 20, _actorUserId);
        session.MarkCompleted(session.EndsAt);
        context.Sessions.Add(session);

        // Present, Late, Absent
        context.AttendanceRecords.Add(AttendanceRecord.CreateCheckIn(session, student1.Id, _workspaceId, session.StartsAt.AddMinutes(5), _actorUserId)); // Present
        context.AttendanceRecords.Add(AttendanceRecord.CreateCheckIn(session, student2.Id, _workspaceId, session.StartsAt.AddMinutes(25), _actorUserId)); // Late
        context.AttendanceRecords.Add(AttendanceRecord.CreateSystemAbsence(session.Id, student3.Id, _workspaceId, session.EndsAt)); // Absent

        await context.SaveChangesAsync();

        var handler = new GetSessionDetailsQueryHandler(new UnitOfWork(context));
        var query = new GetSessionDetailsQuery(_workspaceId, session.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GroupName.Should().Be("Physics A");
        result.Value.RoomName.Should().Be("Lab 3");
        result.Value.PresentCount.Should().Be(1);
        result.Value.LateCount.Should().Be(1);
        result.Value.AbsentCount.Should().Be(1);
        result.Value.TotalRosterCount.Should().Be(3);
    }

    [Fact]
    public async Task GetSessionDetails_WhenSessionNotFound_ShouldReturnNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new GetSessionDetailsQueryHandler(new UnitOfWork(context));
        var query = new GetSessionDetailsQuery(_workspaceId, Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SESSION_NOT_FOUND");
    }
}
