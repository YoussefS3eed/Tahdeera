using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Features.Groups.Commands.EnrollStudentInGroup;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.UnitTests.Features.Groups;

public class EnrollStudentInGroupCommandHandlerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();
    private const short GroupGradeId = 10;

    [Fact]
    public async Task Handle_WhenValid_ShouldEnrollStudentAndRecordHistory()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, GroupGradeId, "Chemistry A", _actorUserId);
        context.Groups.Add(group);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            FullName = "Ahmed Ali",
            StudentCode = "STU-1001"
        };
        context.Students.Add(student);

        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active
        });

        context.StudentAcademicHistories.Add(new StudentAcademicHistory
        {
            StudentId = student.Id,
            AcademicYearId = _academicYearId,
            GradeId = GroupGradeId
        });

        await context.SaveChangesAsync();

        var handler = new EnrollStudentInGroupCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new EnrollStudentInGroupCommand(_workspaceId, group.Id, student.Id, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StudentId.Should().Be(student.Id);
        result.Value.GroupId.Should().Be(group.Id);
        result.Value.Status.Should().Be("Active");

        var enrollmentInDb = await context.GroupStudents.FirstOrDefaultAsync(gs => gs.GroupId == group.Id && gs.StudentId == student.Id);
        enrollmentInDb.Should().NotBeNull();
        enrollmentInDb!.Status.Should().Be(GroupStudentStatus.Active);

        var history = await context.GroupStudentStatusHistories.FirstOrDefaultAsync(h => h.GroupStudentId == enrollmentInDb.Id);
        history.Should().NotBeNull();
        history!.NewStatus.Should().Be(GroupStudentStatus.Active);
        history.Reason.Should().Be("Initial group enrollment");
    }

    [Fact]
    public async Task Handle_WhenGradeMismatch_ShouldReturnError()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, GroupGradeId, "Chemistry A", _actorUserId);
        context.Groups.Add(group);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            FullName = "Sara Mohamed",
            StudentCode = "STU-1002"
        };
        context.Students.Add(student);

        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active
        });

        // Student is Grade 11 instead of 10
        context.StudentAcademicHistories.Add(new StudentAcademicHistory
        {
            StudentId = student.Id,
            AcademicYearId = _academicYearId,
            GradeId = 11
        });

        await context.SaveChangesAsync();

        var handler = new EnrollStudentInGroupCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new EnrollStudentInGroupCommand(_workspaceId, group.Id, student.Id, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GROUP_GRADE_MISMATCH");
    }

    [Fact]
    public async Task Handle_WhenWorkspaceStudentNotActive_ShouldReturnError()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, GroupGradeId, "Chemistry A", _actorUserId);
        context.Groups.Add(group);

        var student = new Student { Id = Guid.NewGuid(), FullName = "Hassan Ibrahim", StudentCode = "STU-1003" };
        context.Students.Add(student);

        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Inactive // Inactive in workspace
        });

        await context.SaveChangesAsync();

        var handler = new EnrollStudentInGroupCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new EnrollStudentInGroupCommand(_workspaceId, group.Id, student.Id, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("STUDENT_WORKSPACE_STATUS_NOT_ACTIVE");
    }

    [Fact]
    public async Task Handle_WhenWithdrawn_ShouldReactivateInPlaceAndAudit()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, GroupGradeId, "Chemistry A", _actorUserId);
        context.Groups.Add(group);

        var student = new Student { Id = Guid.NewGuid(), FullName = "Omar Khaled", StudentCode = "STU-1004" };
        context.Students.Add(student);

        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active
        });

        context.StudentAcademicHistories.Add(new StudentAcademicHistory
        {
            StudentId = student.Id,
            AcademicYearId = _academicYearId,
            GradeId = GroupGradeId
        });

        var existingEnrollment = GroupStudent.Create(group.Id, student.Id, DateTimeOffset.UtcNow.AddMonths(-2));
        existingEnrollment.Withdraw(DateTimeOffset.UtcNow.AddMonths(-1), _actorUserId);
        context.GroupStudents.Add(existingEnrollment);

        await context.SaveChangesAsync();

        var handler = new EnrollStudentInGroupCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new EnrollStudentInGroupCommand(_workspaceId, group.Id, student.Id, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GroupStudentId.Should().Be(existingEnrollment.Id);

        var enrollmentInDb = await context.GroupStudents.FirstAsync(gs => gs.Id == existingEnrollment.Id);
        enrollmentInDb.Status.Should().Be(GroupStudentStatus.Active);
        enrollmentInDb.LeftAt.Should().BeNull();

        var latestHistory = await context.GroupStudentStatusHistories
            .OrderByDescending(h => h.CreatedAt)
            .FirstOrDefaultAsync(h => h.GroupStudentId == existingEnrollment.Id);
        latestHistory.Should().NotBeNull();
        latestHistory!.OldStatus.Should().Be(GroupStudentStatus.Withdrawn);
        latestHistory.NewStatus.Should().Be(GroupStudentStatus.Active);
        latestHistory.Reason.Should().Be("Re-enrolled in group");
    }

    [Fact]
    public async Task Handle_WhenSuspended_ShouldRejectWithConflict()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, GroupGradeId, "Chemistry A", _actorUserId);
        context.Groups.Add(group);

        var student = new Student { Id = Guid.NewGuid(), FullName = "Mona Tarek", StudentCode = "STU-1005" };
        context.Students.Add(student);

        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active
        });

        context.StudentAcademicHistories.Add(new StudentAcademicHistory
        {
            StudentId = student.Id,
            AcademicYearId = _academicYearId,
            GradeId = GroupGradeId
        });

        var existingEnrollment = GroupStudent.Create(group.Id, student.Id, DateTimeOffset.UtcNow.AddMonths(-2));
        existingEnrollment.Suspend(_actorUserId);
        context.GroupStudents.Add(existingEnrollment);

        await context.SaveChangesAsync();

        var handler = new EnrollStudentInGroupCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new EnrollStudentInGroupCommand(_workspaceId, group.Id, student.Id, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("STUDENT_ALREADY_ENROLLED_IN_GROUP");
    }

    [Fact]
    public async Task Handle_WhenAlreadyActive_ShouldBeIdempotentWithoutDuplicateHistory()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, GroupGradeId, "Chemistry A", _actorUserId);
        context.Groups.Add(group);

        var student = new Student { Id = Guid.NewGuid(), FullName = "Layla Nour", StudentCode = "STU-1006" };
        context.Students.Add(student);

        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active
        });

        context.StudentAcademicHistories.Add(new StudentAcademicHistory
        {
            StudentId = student.Id,
            AcademicYearId = _academicYearId,
            GradeId = GroupGradeId
        });

        var existingEnrollment = GroupStudent.Create(group.Id, student.Id, DateTimeOffset.UtcNow.AddMonths(-1));
        context.GroupStudents.Add(existingEnrollment);
        context.GroupStudentStatusHistories.Add(GroupStudentStatusHistory.Record(
            existingEnrollment.Id, null, GroupStudentStatus.Active, DateTimeOffset.UtcNow.AddMonths(-1), _actorUserId, "Initial"));
        await context.SaveChangesAsync();

        var initialHistoryCount = await context.GroupStudentStatusHistories.CountAsync();

        var handler = new EnrollStudentInGroupCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new EnrollStudentInGroupCommand(_workspaceId, group.Id, student.Id, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Active");

        var afterHistoryCount = await context.GroupStudentStatusHistories.CountAsync();
        afterHistoryCount.Should().Be(initialHistoryCount); // No duplicate history entry!
    }
}
