using FluentAssertions;
using Tahdeera.Application.Features.Groups.Commands.CreateGroup;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.UnitTests.Features.Groups;

public class CreateGroupCommandHandlerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_WhenValidGroup_ShouldCreateAndReturnDto()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.AcademicYears.Add(new AcademicYear
        {
            Id = _academicYearId,
            Name = "2026/2027",
            StartsOn = new DateOnly(2026, 9, 1),
            EndsOn = new DateOnly(2027, 6, 30),
            IsCurrent = true
        });
        await context.SaveChangesAsync();

        var handler = new CreateGroupCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new CreateGroupCommand(
            WorkspaceId: _workspaceId,
            AcademicYearId: _academicYearId,
            GradeId: 10,
            SubjectId: null,
            Name: "Physics Group A",
            RoomName: "Room 101",
            ScheduleSummary: "Mon/Wed 5pm",
            LateGraceMinutes: 15,
            DefaultMonthlyFee: 250.00m,
            ConsecutiveAbsenceThreshold: 3,
            AbsenceAction: AbsenceAction.MarkSuspended,
            ActorUserId: _actorUserId,
            TargetGender: "CoEd",
            MaxCapacity: 30);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("Physics Group A");
        result.Value.WorkspaceId.Should().Be(_workspaceId);
        result.Value.LateGraceMinutes.Should().Be(15);
        result.Value.ConsecutiveAbsenceThreshold.Should().Be(3);
        result.Value.AbsenceAction.Should().Be("MarkSuspended");
        result.Value.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_WhenDuplicateNameInSameWorkspaceAndAcademicYear_ShouldReturnConflict()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.AcademicYears.Add(new AcademicYear
        {
            Id = _academicYearId,
            Name = "2026/2027",
            StartsOn = new DateOnly(2026, 9, 1),
            EndsOn = new DateOnly(2027, 6, 30),
            IsCurrent = true
        });
        context.Groups.Add(Group.Create(
            ownerWorkspaceId: _workspaceId,
            academicYearId: _academicYearId,
            gradeId: 10,
            name: "Physics Group A",
            responsibleTeacherUserId: _actorUserId));
        await context.SaveChangesAsync();

        var handler = new CreateGroupCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new CreateGroupCommand(
            WorkspaceId: _workspaceId,
            AcademicYearId: _academicYearId,
            GradeId: 10,
            SubjectId: null,
            Name: "  physics group a  ", // Same name, case & whitespace insensitive
            RoomName: "Room 102",
            ScheduleSummary: null,
            LateGraceMinutes: 20,
            DefaultMonthlyFee: 0,
            ConsecutiveAbsenceThreshold: null,
            AbsenceAction: AbsenceAction.NotifyOnly,
            ActorUserId: _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GROUP_NAME_DUPLICATE");
    }

    [Fact]
    public async Task Handle_WhenAcademicYearNotFound_ShouldReturnNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new CreateGroupCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new CreateGroupCommand(
            WorkspaceId: _workspaceId,
            AcademicYearId: Guid.NewGuid(), // Non-existent
            GradeId: 10,
            SubjectId: null,
            Name: "Biology Group B",
            RoomName: null,
            ScheduleSummary: null,
            LateGraceMinutes: 20,
            DefaultMonthlyFee: 0,
            ConsecutiveAbsenceThreshold: null,
            AbsenceAction: AbsenceAction.NotifyOnly,
            ActorUserId: _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ACADEMIC_YEAR_NOT_FOUND");
    }

    [Fact]
    public void Validator_WhenInvalidGradeOrThreshold_ShouldFailValidation()
    {
        // Arrange
        var validator = new CreateGroupCommandValidator();
        var command = new CreateGroupCommand(
            WorkspaceId: _workspaceId,
            AcademicYearId: _academicYearId,
            GradeId: 13, // Invalid > 12
            SubjectId: null,
            Name: "", // Empty name
            RoomName: null,
            ScheduleSummary: null,
            LateGraceMinutes: -5, // Invalid
            DefaultMonthlyFee: -10, // Invalid
            ConsecutiveAbsenceThreshold: 55, // Invalid > 50
            AbsenceAction: AbsenceAction.NotifyOnly,
            ActorUserId: _actorUserId);

        // Act
        var validationResult = validator.Validate(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorCode == "INVALID_GRADE_ID");
        validationResult.Errors.Should().Contain(e => e.ErrorCode == "INVALID_ABSENCE_THRESHOLD");
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}
