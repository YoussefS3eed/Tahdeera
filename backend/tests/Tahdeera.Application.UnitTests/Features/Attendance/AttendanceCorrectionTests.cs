using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Attendance.Commands.CorrectAttendanceRecord;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Attendance;

public class AttendanceCorrectionTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_Within7Days_ShouldSucceedAndLockAutoEvaluation()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "History A", _actorUserId);
        context.Groups.Add(group);

        // Session completed 3 days ago
        var session = Session.Create(group.Id, baseTime.AddDays(-3), baseTime.AddDays(-3).AddHours(2), 20, _actorUserId);
        session.MarkCompleted(baseTime.AddDays(-3).AddHours(2));
        context.Sessions.Add(session);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_CORR_1", FullName = "Ali Nader" };
        var wsStudent = new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active,
            WhatsappOptIn = true,
            GuardianPhone = "+201055554444"
        };
        var record = AttendanceRecord.CreateSystemAbsence(session.Id, student.Id, _workspaceId, baseTime.AddDays(-3).AddHours(2));

        context.Students.Add(student);
        context.WorkspaceStudents.Add(wsStudent);
        context.AttendanceRecords.Add(record);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime); // 3 days after completion (< 7 days)
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);
        currentUserService.UserId.Returns(_actorUserId);
        var mockReconciliation = Substitute.For<IAttendanceReconciliationService>();

        var handler = new CorrectAttendanceRecordCommandHandler(unitOfWork, timeProvider, currentUserService, mockReconciliation);
        var command = new CorrectAttendanceRecordCommand(record.Id, AttendanceStatus.Present, "Student arrived but door scanner was offline", IsOwner: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NewStatus.Should().Be("Present");
        result.Value.AutoEvaluationLocked.Should().BeTrue();
        result.Value.NotificationStaged.Should().BeTrue();

        var recordInDb = await context.AttendanceRecords.FirstAsync(r => r.Id == record.Id);
        recordInDb.Status.Should().Be(AttendanceStatus.Present);
        recordInDb.AutoEvaluationLocked.Should().BeTrue();

        var historyInDb = await context.AttendanceHistories.FirstAsync(h => h.AttendanceRecordId == record.Id && h.ChangeKind == AttendanceChangeKind.ManualCorrection);
        historyInDb.OldStatus.Should().Be(AttendanceStatus.Absent);
        historyInDb.NewStatus.Should().Be(AttendanceStatus.Present);
        historyInDb.Reason.Should().Be("Student arrived but door scanner was offline");
    }

    [Fact]
    public async Task Handle_Past7Days_WhenNotOwner_ShouldFailWithExpiredWindow()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "History A", _actorUserId);
        context.Groups.Add(group);

        // Session completed 8 days ago
        var session = Session.Create(group.Id, baseTime.AddDays(-8), baseTime.AddDays(-8).AddHours(2), 20, _actorUserId);
        session.MarkCompleted(baseTime.AddDays(-8).AddHours(2));
        context.Sessions.Add(session);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_CORR_2", FullName = "Ali Nader" };
        var record = AttendanceRecord.CreateSystemAbsence(session.Id, student.Id, _workspaceId, baseTime.AddDays(-8).AddHours(2));

        context.Students.Add(student);
        context.AttendanceRecords.Add(record);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime); // 8 days after completion (> 7 days)
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);
        currentUserService.UserId.Returns(_actorUserId);
        var mockReconciliation = Substitute.For<IAttendanceReconciliationService>();

        var handler = new CorrectAttendanceRecordCommandHandler(unitOfWork, timeProvider, currentUserService, mockReconciliation);
        var command = new CorrectAttendanceRecordCommand(record.Id, AttendanceStatus.Present, "Correction requested late by teacher", IsOwner: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ATTENDANCE_CORRECTION_WINDOW_EXPIRED");
    }

    [Fact]
    public async Task Handle_Past7Days_WhenWorkspaceOwner_ShouldSucceedWithOverride()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "History A", _actorUserId);
        context.Groups.Add(group);

        // Session completed 10 days ago
        var session = Session.Create(group.Id, baseTime.AddDays(-10), baseTime.AddDays(-10).AddHours(2), 20, _actorUserId);
        session.MarkCompleted(baseTime.AddDays(-10).AddHours(2));
        context.Sessions.Add(session);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_CORR_3", FullName = "Ali Nader" };
        var record = AttendanceRecord.CreateSystemAbsence(session.Id, student.Id, _workspaceId, baseTime.AddDays(-10).AddHours(2));

        context.Students.Add(student);
        context.AttendanceRecords.Add(record);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime); // 10 days after completion (> 7 days)
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);
        currentUserService.UserId.Returns(_actorUserId);
        var mockReconciliation = Substitute.For<IAttendanceReconciliationService>();

        var handler = new CorrectAttendanceRecordCommandHandler(unitOfWork, timeProvider, currentUserService, mockReconciliation);
        var command = new CorrectAttendanceRecordCommand(record.Id, AttendanceStatus.Excused, "Center Owner approved medical excusal", IsOwner: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NewStatus.Should().Be("Excused");
        result.Value.AutoEvaluationLocked.Should().BeTrue();

        var recordInDb = await context.AttendanceRecords.FirstAsync(r => r.Id == record.Id);
        recordInDb.Status.Should().Be(AttendanceStatus.Excused);
    }

    [Fact]
    public async Task Handle_WithoutReason_ShouldFailWithCorrectionReasonRequired()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "History A", _actorUserId);
        context.Groups.Add(group);
        var session = Session.Create(group.Id, baseTime.AddDays(-1), baseTime.AddDays(-1).AddHours(2), 20, _actorUserId);
        context.Sessions.Add(session);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_CORR_4", FullName = "Ali Nader" };
        var record = AttendanceRecord.CreateSystemAbsence(session.Id, student.Id, _workspaceId, baseTime.AddDays(-1).AddHours(2));

        context.Students.Add(student);
        context.AttendanceRecords.Add(record);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime);
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);
        var mockReconciliation = Substitute.For<IAttendanceReconciliationService>();

        var handler = new CorrectAttendanceRecordCommandHandler(unitOfWork, timeProvider, currentUserService, mockReconciliation);
        var command = new CorrectAttendanceRecordCommand(record.Id, AttendanceStatus.Present, Reason: "   ", IsOwner: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("CORRECTION_REASON_REQUIRED");
    }
}
