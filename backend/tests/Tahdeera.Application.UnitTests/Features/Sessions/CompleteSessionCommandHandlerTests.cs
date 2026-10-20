using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Sessions.Commands.CompleteSession;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Sessions;

public class CompleteSessionCommandHandlerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();
    private readonly IDateTimeProvider _dateTimeProvider;

    public CompleteSessionCommandHandlerTests()
    {
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    }

    [Fact]
    public async Task Handle_WhenPrematureCompletionBeforeStartsAt_ShouldRejectWith400AndNoSideEffects()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Math A", _actorUserId);
        context.Groups.Add(group);

        var startsAt = DateTimeOffset.UtcNow.AddHours(2);
        var endsAt = startsAt.AddHours(2);
        var session = Session.Create(group.Id, startsAt, endsAt, 15, _actorUserId);
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        // Server time is 1 hour before startsAt
        _dateTimeProvider.UtcNow.Returns(startsAt.AddHours(-1));

        var handler = new CompleteSessionCommandHandler(new UnitOfWork(context), _dateTimeProvider);
        var command = new CompleteSessionCommand(_workspaceId, session.Id, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SESSION_CANNOT_COMPLETE_BEFORE_START");

        var sessionInDb = await context.Sessions.FirstAsync(s => s.Id == session.Id);
        sessionInDb.Status.Should().Be(SessionStatus.Scheduled);
        sessionInDb.AutoAbsenceProcessedAt.Should().BeNull();

        var attendanceCount = await context.AttendanceRecords.CountAsync();
        attendanceCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenCompletionAfterStart_ShouldGenerateAbsencesAndStageOutbox()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Math A", _actorUserId);
        context.Groups.Add(group);

        var student1 = new Student { Id = Guid.NewGuid(), FullName = "Student One", StudentCode = "STU-01" };
        var student2 = new Student { Id = Guid.NewGuid(), FullName = "Student Two", StudentCode = "STU-02" };
        context.Students.AddRange(student1, student2);

        var e1 = GroupStudent.Create(group.Id, student1.Id, DateTimeOffset.UtcNow.AddMonths(-1));
        var e2 = GroupStudent.Create(group.Id, student2.Id, DateTimeOffset.UtcNow.AddMonths(-1));
        context.GroupStudents.AddRange(e1, e2);

        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student1.Id,
            GuardianPhone = "+201011111111",
            WhatsappOptIn = true,
            Status = WorkspaceStudentStatus.Active
        });

        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student2.Id,
            GuardianPhone = "+201022222222",
            WhatsappOptIn = false, // opted out
            Status = WorkspaceStudentStatus.Active
        });

        var startsAt = DateTimeOffset.UtcNow.AddHours(-2);
        var endsAt = startsAt.AddHours(2);
        var session = Session.Create(group.Id, startsAt, endsAt, 15, _actorUserId);
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        // Server time is at scheduled end time
        _dateTimeProvider.UtcNow.Returns(endsAt);

        var handler = new CompleteSessionCommandHandler(new UnitOfWork(context), _dateTimeProvider);
        var command = new CompleteSessionCommand(_workspaceId, session.Id, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalActiveRoster.Should().Be(2);
        result.Value.GeneratedAbsencesCount.Should().Be(2);
        result.Value.StagedOutboxMessagesCount.Should().Be(1); // Only student 1 opted in

        var sessionInDb = await context.Sessions.FirstAsync(s => s.Id == session.Id);
        sessionInDb.Status.Should().Be(SessionStatus.Completed);

        var outboxMessage = await context.MessageOutboxes.FirstOrDefaultAsync();
        outboxMessage.Should().NotBeNull();
        outboxMessage!.StudentId.Should().Be(student1.Id);
        outboxMessage.RecipientPhone.Should().Be("+201011111111");
        outboxMessage.IdempotencyKey.Should().StartWith("att_");
        outboxMessage.IdempotencyKey.Should().EndWith("_Absent");
    }

    [Fact]
    public async Task Handle_WhenStudentReachesAbsenceThreshold_ShouldSuspendAndRecordHistory()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(
            _workspaceId,
            _academicYearId,
            10,
            "Physics A",
            _actorUserId,
            consecutiveAbsenceThreshold: 2,
            absenceAction: AbsenceAction.MarkSuspended);
        context.Groups.Add(group);

        var student = new Student { Id = Guid.NewGuid(), FullName = "Chronic Absentee", StudentCode = "STU-99" };
        context.Students.Add(student);

        var enrollment = GroupStudent.Create(group.Id, student.Id, DateTimeOffset.UtcNow.AddMonths(-1));
        context.GroupStudents.Add(enrollment);

        // Add 1 prior completed session with an Absent record
        var priorSession = Session.Create(group.Id, DateTimeOffset.UtcNow.AddDays(-3), DateTimeOffset.UtcNow.AddDays(-3).AddHours(2));
        priorSession.MarkCompleted(priorSession.EndsAt);
        context.Sessions.Add(priorSession);
        context.AttendanceRecords.Add(AttendanceRecord.CreateSystemAbsence(priorSession.Id, student.Id, _workspaceId, priorSession.EndsAt));

        // Current session to be completed
        var currentStartsAt = DateTimeOffset.UtcNow.AddHours(-1);
        var currentEndsAt = currentStartsAt.AddHours(2);
        var currentSession = Session.Create(group.Id, currentStartsAt, currentEndsAt, 15, _actorUserId);
        context.Sessions.Add(currentSession);
        await context.SaveChangesAsync();

        _dateTimeProvider.UtcNow.Returns(currentEndsAt);

        var handler = new CompleteSessionCommandHandler(new UnitOfWork(context), _dateTimeProvider);
        var command = new CompleteSessionCommand(_workspaceId, currentSession.Id, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StudentsSuspendedCount.Should().Be(1);

        var studentInDb = await context.GroupStudents.FirstAsync(gs => gs.Id == enrollment.Id);
        studentInDb.Status.Should().Be(GroupStudentStatus.Suspended);

        var statusHistory = await context.GroupStudentStatusHistories
            .FirstOrDefaultAsync(h => h.GroupStudentId == enrollment.Id && h.NewStatus == GroupStudentStatus.Suspended);
        statusHistory.Should().NotBeNull();
        statusHistory!.Reason.Should().Be("Consecutive absence threshold reached");
    }
}
