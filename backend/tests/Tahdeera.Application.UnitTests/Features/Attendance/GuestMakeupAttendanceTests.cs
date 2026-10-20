using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Attendance.Commands.RegisterGuestMakeupAttendance;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Attendance;

public class GuestMakeupAttendanceTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidMakeup_ShouldCreateGuestRecord_AutoExcuseHomeSession_AndZeroVisitedEnrollment()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        // Visited Group (Group B)
        var visitedGroup = Group.Create(_workspaceId, _academicYearId, 11, "Physics Sunday", _actorUserId);
        // Home Group (Group A)
        var homeGroup = Group.Create(_workspaceId, _academicYearId, 11, "Physics Tuesday", _actorUserId);
        context.Groups.AddRange(visitedGroup, homeGroup);

        // Student active in Home Group
        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_MAKEUP", FullName = "Mostafa Kamel" };
        var wsStudent = new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active,
            WhatsappOptIn = true,
            GuardianPhone = "+201011223344"
        };
        var homeEnrollment = GroupStudent.Create(homeGroup.Id, student.Id, baseTime.AddDays(-20));

        // Past Completed Session in Home Group with Absent record
        var homePastSession = Session.Create(homeGroup.Id, baseTime.AddDays(-3), baseTime.AddDays(-3).AddHours(2), 20, _actorUserId);
        homePastSession.MarkCompleted(baseTime.AddDays(-3).AddHours(2));
        var homePastRecord = AttendanceRecord.CreateSystemAbsence(homePastSession.Id, student.Id, _workspaceId, baseTime.AddDays(-3).AddHours(2));

        // Current Visited Session
        var visitedSession = Session.Create(visitedGroup.Id, baseTime, baseTime.AddHours(2), 20, _actorUserId);

        context.Students.Add(student);
        context.WorkspaceStudents.Add(wsStudent);
        context.GroupStudents.Add(homeEnrollment);
        context.Sessions.AddRange(homePastSession, visitedSession);
        context.AttendanceRecords.Add(homePastRecord);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime.AddMinutes(5));
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);
        currentUserService.UserId.Returns(_actorUserId);
        var mockReconciliation = Substitute.For<IAttendanceReconciliationService>();

        var handler = new RegisterGuestMakeupAttendanceCommandHandler(unitOfWork, timeProvider, currentUserService, mockReconciliation);
        var command = new RegisterGuestMakeupAttendanceCommand(visitedSession.Id, student.Id, homeGroup.Id, HomeSessionId: null, Reason: "Attending make-up for missed session");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsGuestMakeup.Should().BeTrue();
        result.Value.HomeSessionExcused.Should().BeTrue();
        result.Value.ExcusedHomeSessionId.Should().Be(homePastSession.Id);
        result.Value.GuestInvoicesCreated.Should().Be(0);
        result.Value.BillingStatus.Should().Be("CoveredByHomeSubscription");

        // Assert Roster Isolation (FR-11): Group B has 0 enrollments
        var visitedGroupStudentsCount = await context.GroupStudents.CountAsync(gs => gs.GroupId == visitedGroup.Id);
        visitedGroupStudentsCount.Should().Be(0);

        // Assert Home group session excused (FR-13)
        var homeRecordInDb = await context.AttendanceRecords.FirstAsync(r => r.Id == homePastRecord.Id);
        homeRecordInDb.Status.Should().Be(AttendanceStatus.Excused);
        homeRecordInDb.RetroactiveReason.Should().Contain("Physics Sunday");

        // Assert streak reconciliation was triggered
        await mockReconciliation.Received(1).ReconcileStudentStreakAsync(homeGroup.Id, student.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExplicitHomeSessionId_WhenAlreadyExcusedOrPresent_ShouldFail()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var visitedGroup = Group.Create(_workspaceId, _academicYearId, 11, "Physics Sunday", _actorUserId);
        var homeGroup = Group.Create(_workspaceId, _academicYearId, 11, "Physics Tuesday", _actorUserId);
        context.Groups.AddRange(visitedGroup, homeGroup);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_MAKEUP_2", FullName = "Hany Shaker" };
        var wsStudent = new WorkspaceStudent { WorkspaceId = _workspaceId, StudentId = student.Id, Status = WorkspaceStudentStatus.Active };
        var homeEnrollment = GroupStudent.Create(homeGroup.Id, student.Id, baseTime.AddDays(-20));

        // Home session where student was Present (not Absent)
        var homePastSession = Session.Create(homeGroup.Id, baseTime.AddDays(-3), baseTime.AddDays(-3).AddHours(2), 20, _actorUserId);
        homePastSession.MarkCompleted(baseTime.AddDays(-3).AddHours(2));
        var homePastRecord = AttendanceRecord.CreateCheckIn(homePastSession, student.Id, _workspaceId, homePastSession.StartsAt, _actorUserId);

        var visitedSession = Session.Create(visitedGroup.Id, baseTime, baseTime.AddHours(2), 20, _actorUserId);

        context.Students.Add(student);
        context.WorkspaceStudents.Add(wsStudent);
        context.GroupStudents.Add(homeEnrollment);
        context.Sessions.AddRange(homePastSession, visitedSession);
        context.AttendanceRecords.Add(homePastRecord);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime.AddMinutes(5));
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);
        var mockReconciliation = Substitute.For<IAttendanceReconciliationService>();

        var handler = new RegisterGuestMakeupAttendanceCommandHandler(unitOfWork, timeProvider, currentUserService, mockReconciliation);
        var command = new RegisterGuestMakeupAttendanceCommand(visitedSession.Id, student.Id, homeGroup.Id, HomeSessionId: homePastSession.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("HOME_SESSION_NOT_ELIGIBLE_FOR_EXCUSAL");
    }

    [Fact]
    public async Task Handle_WhenStudentNotEnrolledInHomeGroup_ShouldFail()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var visitedGroup = Group.Create(_workspaceId, _academicYearId, 11, "Physics Sunday", _actorUserId);
        var homeGroup = Group.Create(_workspaceId, _academicYearId, 11, "Physics Tuesday", _actorUserId);
        context.Groups.AddRange(visitedGroup, homeGroup);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_NOT_ENROLLED", FullName = "Guest Student" };
        var wsStudent = new WorkspaceStudent { WorkspaceId = _workspaceId, StudentId = student.Id, Status = WorkspaceStudentStatus.Active };
        var visitedSession = Session.Create(visitedGroup.Id, baseTime, baseTime.AddHours(2), 20, _actorUserId);

        context.Students.Add(student);
        context.WorkspaceStudents.Add(wsStudent);
        context.Sessions.Add(visitedSession);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime);
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);
        var mockReconciliation = Substitute.For<IAttendanceReconciliationService>();

        var handler = new RegisterGuestMakeupAttendanceCommandHandler(unitOfWork, timeProvider, currentUserService, mockReconciliation);
        var command = new RegisterGuestMakeupAttendanceCommand(visitedSession.Id, student.Id, homeGroup.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("STUDENT_NOT_ENROLLED_IN_HOME_GROUP");
    }
}
