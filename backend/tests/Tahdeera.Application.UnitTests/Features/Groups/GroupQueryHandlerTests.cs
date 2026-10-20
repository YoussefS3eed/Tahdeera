using FluentAssertions;
using Tahdeera.Application.Features.Groups.Queries.GetGroupDetails;
using Tahdeera.Application.Features.Groups.Queries.GetGroupRoster;
using Tahdeera.Application.Features.Groups.Queries.GetGroups;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.UnitTests.Features.Groups;

public class GroupQueryHandlerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _teacherUserId = Guid.NewGuid();

    [Fact]
    public async Task GetGroups_ShouldFilterByWorkspaceAndStatus()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var g1 = Group.Create(_workspaceId, _academicYearId, 10, "Math A", _teacherUserId);
        var g2 = Group.Create(_workspaceId, _academicYearId, 10, "Math B", _teacherUserId);
        g2.SetStatus(GroupStatus.Archived, _teacherUserId);
        var g3 = Group.Create(Guid.NewGuid(), _academicYearId, 10, "Other Workspace Group", _teacherUserId);
        context.Groups.AddRange(g1, g2, g3);
        await context.SaveChangesAsync();

        var handler = new GetGroupsQueryHandler(new Tahdeera.Infrastructure.Persistence.Repositories.GroupRepository(context));
        var query = new GetGroupsQuery(_workspaceId, Status: GroupStatus.Active);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Math A");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetGroupDetails_ShouldReturnFullProfileAndDelegate()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var academicYear = new AcademicYear
        {
            Id = _academicYearId,
            Name = "2026/2027",
            StartsOn = new DateOnly(2026, 9, 1),
            EndsOn = new DateOnly(2027, 6, 30),
            IsCurrent = true
        };
        context.AcademicYears.Add(academicYear);

        var grade = new Grade { Id = 10, Code = "SEC1", NameAr = "الأول الثانوي", NameEn = "Sec 1", SortOrder = 10, IsActive = true };
        context.Grades.Add(grade);

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _teacherUserId);
        context.Groups.Add(group);

        var assistantId = Guid.NewGuid();
        context.AppUsers.Add(new AppUser { Id = assistantId, DisplayName = "Sarah Assistant" });

        var delegation = GroupAttendanceDelegation.Create(group.Id, assistantId, _teacherUserId);
        context.GroupAttendanceDelegations.Add(delegation);
        await context.SaveChangesAsync();

        var handler = new GetGroupDetailsQueryHandler(new Tahdeera.Infrastructure.Persistence.Repositories.GroupRepository(context));
        var query = new GetGroupDetailsQuery(_workspaceId, group.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Physics A");
        result.Value.ActiveDelegate.Should().NotBeNull();
        result.Value.ActiveDelegate!.AssistantName.Should().Be("Sarah Assistant");
    }

    [Fact]
    public async Task GetGroupRoster_WhenAuthorizedOwner_ShouldReturnRosterWithCounters()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Biology A", _teacherUserId);
        context.Groups.Add(group);

        var student = new Student { Id = Guid.NewGuid(), FullName = "Kareem Taha", StudentCode = "STU-88" };
        context.Students.Add(student);

        var enrollment = GroupStudent.Create(group.Id, student.Id, DateTimeOffset.UtcNow);
        context.GroupStudents.Add(enrollment);

        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student.Id,
            GuardianPhone = "+201234567890",
            Status = WorkspaceStudentStatus.Active
        });

        // Add 1 completed session and 1 attended record
        var session = Session.Create(group.Id, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(-1).AddHours(2));
        session.MarkCompleted(session.EndsAt);
        context.Sessions.Add(session);

        var record = AttendanceRecord.CreateCheckIn(session, student.Id, _workspaceId, session.StartsAt.AddMinutes(5), _teacherUserId);
        context.AttendanceRecords.Add(record);

        await context.SaveChangesAsync();

        var handler = new GetGroupRosterQueryHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var query = new GetGroupRosterQuery(_workspaceId, group.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].StudentName.Should().Be("Kareem Taha");
        result.Value.Items[0].GuardianPhone.Should().Be("+201234567890");
        result.Value.Items[0].TotalAttendedSessions.Should().Be(1);
        result.Value.Items[0].TotalAbsentSessions.Should().Be(0);
    }

    [Fact]
    public async Task GetGroupRoster_WhenUnauthorizedWorkspace_ShouldReturnForbidden()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Biology A", _teacherUserId);
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var randomWorkspaceId = Guid.NewGuid();
        var handler = new GetGroupRosterQueryHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var query = new GetGroupRosterQuery(randomWorkspaceId, group.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHORIZED_WORKSPACE_ACCESS");
    }
}
