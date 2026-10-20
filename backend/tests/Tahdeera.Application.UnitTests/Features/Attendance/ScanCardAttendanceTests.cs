using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Attendance.Commands.ScanCardAttendance;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Attendance;

public class ScanCardAttendanceTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_PunctualityBoundary_ShouldReturnPresentAtGraceAndLatePastGrace()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);
        var lateGraceMinutes = 15;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);

        var session = Session.Create(group.Id, baseTime, baseTime.AddHours(2), lateGraceMinutes, _actorUserId);
        context.Sessions.Add(session);

        // Student 1 (On Time / Exact Grace)
        var student1 = new Student { Id = Guid.NewGuid(), StudentCode = "STU001", FullName = "Ahmed Ali" };
        var wsStudent1 = new WorkspaceStudent { WorkspaceId = _workspaceId, StudentId = student1.Id, Status = WorkspaceStudentStatus.Active };
        var rawToken1 = "CARD_TOKEN_1";
        var tokenHash1 = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken1));
        var cred1 = new StudentCredential { Id = Guid.NewGuid(), WorkspaceId = _workspaceId, StudentId = student1.Id, TokenHash = tokenHash1, CredentialType = CredentialType.Nfc };

        // Student 2 (Late by 1 second past grace)
        var student2 = new Student { Id = Guid.NewGuid(), StudentCode = "STU002", FullName = "Omar Khaled" };
        var wsStudent2 = new WorkspaceStudent { WorkspaceId = _workspaceId, StudentId = student2.Id, Status = WorkspaceStudentStatus.Active };
        var rawToken2 = "CARD_TOKEN_2";
        var tokenHash2 = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken2));
        var cred2 = new StudentCredential { Id = Guid.NewGuid(), WorkspaceId = _workspaceId, StudentId = student2.Id, TokenHash = tokenHash2, CredentialType = CredentialType.Nfc };

        context.Students.AddRange(student1, student2);
        context.WorkspaceStudents.AddRange(wsStudent1, wsStudent2);
        context.StudentCredentials.AddRange(cred1, cred2);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);
        currentUserService.UserId.Returns(_actorUserId);

        // Act 1: Check in at exact grace deadline (10:15:00)
        timeProvider.UtcNow.Returns(baseTime.AddMinutes(lateGraceMinutes));
        var handler1 = new ScanCardAttendanceCommandHandler(unitOfWork, timeProvider, currentUserService);
        var result1 = await handler1.Handle(new ScanCardAttendanceCommand(session.Id, rawToken1), CancellationToken.None);

        // Act 2: Check in 1 second past grace (10:15:01)
        timeProvider.UtcNow.Returns(baseTime.AddMinutes(lateGraceMinutes).AddSeconds(1));
        var handler2 = new ScanCardAttendanceCommandHandler(unitOfWork, timeProvider, currentUserService);
        var result2 = await handler2.Handle(new ScanCardAttendanceCommand(session.Id, rawToken2), CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result1.Value.Status.Should().Be("Present");

        result2.IsSuccess.Should().BeTrue();
        result2.Value.Status.Should().Be("Late");
    }

    [Fact]
    public async Task Handle_DuplicateScan_ShouldBeIdempotentAndNotStageDuplicateOutbox()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);

        var session = Session.Create(group.Id, baseTime, baseTime.AddHours(2), 15, _actorUserId);
        context.Sessions.Add(session);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU001", FullName = "Ahmed Ali" };
        var wsStudent = new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active,
            WhatsappOptIn = true,
            GuardianPhone = "+201012345678"
        };
        var rawToken = "CARD_TOKEN_TAP_TWICE";
        var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        var cred = new StudentCredential { Id = Guid.NewGuid(), WorkspaceId = _workspaceId, StudentId = student.Id, TokenHash = tokenHash, CredentialType = CredentialType.Nfc };

        context.Students.Add(student);
        context.WorkspaceStudents.Add(wsStudent);
        context.StudentCredentials.Add(cred);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime.AddMinutes(5));
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);
        currentUserService.UserId.Returns(_actorUserId);

        var handler = new ScanCardAttendanceCommandHandler(unitOfWork, timeProvider, currentUserService);
        var command = new ScanCardAttendanceCommand(session.Id, rawToken);

        // Act: Tap 1
        var result1 = await handler.Handle(command, CancellationToken.None);

        // Act: Tap 2 (rapid duplicate tap 3 seconds later)
        timeProvider.UtcNow.Returns(baseTime.AddMinutes(5).AddSeconds(3));
        var result2 = await handler.Handle(command, CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result1.Value.NotificationStaged.Should().BeTrue();

        result2.IsSuccess.Should().BeTrue();
        result2.Value.NotificationStaged.Should().BeFalse();
        result2.Value.AttendanceRecordId.Should().Be(result1.Value.AttendanceRecordId);

        var attendanceCount = await context.AttendanceRecords.CountAsync(r => r.SessionId == session.Id && r.StudentId == student.Id);
        attendanceCount.Should().Be(1);

        var outboxCount = await context.MessageOutboxes.CountAsync(m => m.StudentId == student.Id);
        outboxCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenCredentialRevoked_ShouldReturnForbiddenError()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);
        var session = Session.Create(group.Id, baseTime, baseTime.AddHours(2), 15, _actorUserId);
        context.Sessions.Add(session);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU003", FullName = "Revoked User" };
        var rawToken = "REVOKED_CARD";
        var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        var cred = new StudentCredential
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _workspaceId,
            StudentId = student.Id,
            TokenHash = tokenHash,
            RevokedAt = baseTime.AddDays(-1),
            CredentialType = CredentialType.Nfc
        };

        context.Students.Add(student);
        context.StudentCredentials.Add(cred);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime);
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);

        var handler = new ScanCardAttendanceCommandHandler(unitOfWork, timeProvider, currentUserService);

        // Act
        var result = await handler.Handle(new ScanCardAttendanceCommand(session.Id, rawToken), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("CREDENTIAL_REVOKED");
    }

    [Fact]
    public async Task Handle_WhenStudentBlockedInWorkspace_ShouldReturnForbidden()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);
        var session = Session.Create(group.Id, baseTime, baseTime.AddHours(2), 15, _actorUserId);
        context.Sessions.Add(session);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU004", FullName = "Blocked User" };
        var wsStudent = new WorkspaceStudent { WorkspaceId = _workspaceId, StudentId = student.Id, Status = WorkspaceStudentStatus.Blocked };
        var rawToken = "BLOCKED_CARD";
        var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        var cred = new StudentCredential { Id = Guid.NewGuid(), WorkspaceId = _workspaceId, StudentId = student.Id, TokenHash = tokenHash, CredentialType = CredentialType.Nfc };

        context.Students.Add(student);
        context.WorkspaceStudents.Add(wsStudent);
        context.StudentCredentials.Add(cred);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime);
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.ActiveWorkspaceId.Returns(_workspaceId);

        var handler = new ScanCardAttendanceCommandHandler(unitOfWork, timeProvider, currentUserService);

        // Act
        var result = await handler.Handle(new ScanCardAttendanceCommand(session.Id, rawToken), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("STUDENT_BLOCKED_IN_WORKSPACE");
    }
}
