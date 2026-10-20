using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Attendance.Commands.ManualCheckIn;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Attendance;

public class ManualCheckInTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ActiveEnrolledStudent_ShouldSucceedAndStageWhatsApp()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Math B", _actorUserId);
        context.Groups.Add(group);

        var session = Session.Create(group.Id, baseTime, baseTime.AddHours(2), 20, _actorUserId);
        context.Sessions.Add(session);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_MANUAL_1", FullName = "Hassan Ibrahim" };
        var wsStudent = new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active,
            WhatsappOptIn = true,
            GuardianPhone = "+201099887766"
        };
        var groupStudent = GroupStudent.Create(group.Id, student.Id, baseTime.AddDays(-10));

        context.Students.Add(student);
        context.WorkspaceStudents.Add(wsStudent);
        context.GroupStudents.Add(groupStudent);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime.AddMinutes(5));
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);
        currentUserService.UserId.Returns(_actorUserId);

        var handler = new ManualCheckInCommandHandler(unitOfWork, timeProvider, currentUserService);
        var command = new ManualCheckInCommand(session.Id, student.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Present");
        result.Value.NotificationStaged.Should().BeTrue();

        var recordInDb = await context.AttendanceRecords.FirstAsync(r => r.SessionId == session.Id && r.StudentId == student.Id);
        recordInDb.Source.Should().Be(AttendanceSource.User);
        recordInDb.RecordedByUserId.Should().Be(_actorUserId);

        var historyInDb = await context.AttendanceHistories.FirstAsync(h => h.AttendanceRecordId == recordInDb.Id);
        historyInDb.ChangeKind.Should().Be(AttendanceChangeKind.InitialUserEntry);

        var outboxInDb = await context.MessageOutboxes.FirstAsync(m => m.AttendanceRecordId == recordInDb.Id);
        outboxInDb.RecipientPhone.Should().Be("+201099887766");
    }

    [Fact]
    public async Task Handle_WithdrawnStudentWithoutReason_ShouldFailWithRetroactiveReasonRequired()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Math B", _actorUserId);
        context.Groups.Add(group);

        var session = Session.Create(group.Id, baseTime, baseTime.AddHours(2), 20, _actorUserId);
        context.Sessions.Add(session);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_WITHDRAWN", FullName = "Youssef Zaki" };
        var wsStudent = new WorkspaceStudent { WorkspaceId = _workspaceId, StudentId = student.Id, Status = WorkspaceStudentStatus.Active };
        var groupStudent = GroupStudent.Create(group.Id, student.Id, baseTime.AddDays(-10));
        groupStudent.Withdraw(baseTime.AddDays(-1), _actorUserId);

        context.Students.Add(student);
        context.WorkspaceStudents.Add(wsStudent);
        context.GroupStudents.Add(groupStudent);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime.AddMinutes(5));
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);

        var handler = new ManualCheckInCommandHandler(unitOfWork, timeProvider, currentUserService);
        var command = new ManualCheckInCommand(session.Id, student.Id, RetroactiveReason: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RETROACTIVE_REASON_REQUIRED");
    }

    [Fact]
    public async Task Handle_WithdrawnStudentWithReason_ShouldSucceed()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Math B", _actorUserId);
        context.Groups.Add(group);

        var session = Session.Create(group.Id, baseTime, baseTime.AddHours(2), 20, _actorUserId);
        context.Sessions.Add(session);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_WITHDRAWN_2", FullName = "Youssef Zaki" };
        var wsStudent = new WorkspaceStudent { WorkspaceId = _workspaceId, StudentId = student.Id, Status = WorkspaceStudentStatus.Active };
        var groupStudent = GroupStudent.Create(group.Id, student.Id, baseTime.AddDays(-10));
        groupStudent.Withdraw(baseTime.AddDays(-1), _actorUserId);

        context.Students.Add(student);
        context.WorkspaceStudents.Add(wsStudent);
        context.GroupStudents.Add(groupStudent);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime.AddMinutes(5));
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);

        var handler = new ManualCheckInCommandHandler(unitOfWork, timeProvider, currentUserService);
        var command = new ManualCheckInCommand(session.Id, student.Id, RetroactiveReason: "Attended session before withdrawal processed");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var recordInDb = await context.AttendanceRecords.FirstAsync(r => r.SessionId == session.Id && r.StudentId == student.Id);
        recordInDb.RetroactiveReason.Should().Be("Attended session before withdrawal processed");
    }

    [Fact]
    public async Task Handle_SuspendedStudent_ShouldFailWithSuspendedError()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Math B", _actorUserId);
        context.Groups.Add(group);

        var session = Session.Create(group.Id, baseTime, baseTime.AddHours(2), 20, _actorUserId);
        context.Sessions.Add(session);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_SUSPENDED", FullName = "Karim Tarek" };
        var wsStudent = new WorkspaceStudent { WorkspaceId = _workspaceId, StudentId = student.Id, Status = WorkspaceStudentStatus.Active };
        var groupStudent = GroupStudent.Create(group.Id, student.Id, baseTime.AddDays(-10));
        groupStudent.Suspend(_actorUserId);

        context.Students.Add(student);
        context.WorkspaceStudents.Add(wsStudent);
        context.GroupStudents.Add(groupStudent);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime.AddMinutes(5));
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);

        var handler = new ManualCheckInCommandHandler(unitOfWork, timeProvider, currentUserService);
        var command = new ManualCheckInCommand(session.Id, student.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("STUDENT_ALREADY_ENROLLED_IN_GROUP");
    }
}
