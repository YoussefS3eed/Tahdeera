using FluentAssertions;
using Tahdeera.Application.Features.Groups.Commands.AssignAttendanceDelegate;
using Tahdeera.Application.Features.Groups.Commands.ChangeGroupStatus;
using Tahdeera.Application.Features.Groups.Commands.ChangeGroupStudentStatus;
using Tahdeera.Application.Features.Groups.Commands.CreateGroup;
using Tahdeera.Application.Features.Groups.Commands.EnrollStudentInGroup;
using Tahdeera.Application.Features.Groups.Commands.GroupWorkspaceAccess;
using Tahdeera.Application.Features.Groups.Commands.UpdateGroup;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.UnitTests.Validators;

public class GroupsValidatorTests
{
    [Fact]
    public void CreateGroupCommandValidator_ValidCommand_Passes()
    {
        var validator = new CreateGroupCommandValidator();
        var cmd = new CreateGroupCommand(
            WorkspaceId: Guid.NewGuid(),
            AcademicYearId: Guid.NewGuid(),
            GradeId: 3,
            SubjectId: Guid.NewGuid(),
            Name: "مجموعة الرياضيات المتقدمة",
            RoomName: "قاعة 1",
            ScheduleSummary: "السبت والثلاثاء 4 مساءً",
            LateGraceMinutes: 15,
            DefaultMonthlyFee: 400.00m,
            ConsecutiveAbsenceThreshold: 3,
            AbsenceAction: AbsenceAction.MarkSuspended,
            ActorUserId: Guid.NewGuid(),
            TargetGender: "CoEd",
            MaxCapacity: 35);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-5)]
    public void CreateGroupCommandValidator_InvalidGrade_Fails(short gradeId)
    {
        var validator = new CreateGroupCommandValidator();
        var cmd = new CreateGroupCommand(
            WorkspaceId: Guid.NewGuid(),
            AcademicYearId: Guid.NewGuid(),
            GradeId: gradeId,
            SubjectId: null,
            Name: "Group",
            RoomName: null,
            ScheduleSummary: null,
            LateGraceMinutes: 10,
            DefaultMonthlyFee: 100,
            ConsecutiveAbsenceThreshold: null,
            AbsenceAction: AbsenceAction.NotifyOnly,
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateGroupCommand.GradeId));
    }

    [Fact]
    public void CreateGroupCommandValidator_NegativeFee_Fails()
    {
        var validator = new CreateGroupCommandValidator();
        var cmd = new CreateGroupCommand(
            WorkspaceId: Guid.NewGuid(),
            AcademicYearId: Guid.NewGuid(),
            GradeId: 1,
            SubjectId: null,
            Name: "Group",
            RoomName: null,
            ScheduleSummary: null,
            LateGraceMinutes: 10,
            DefaultMonthlyFee: -50m,
            ConsecutiveAbsenceThreshold: null,
            AbsenceAction: AbsenceAction.NotifyOnly,
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateGroupCommand.DefaultMonthlyFee));
    }

    [Fact]
    public void UpdateGroupCommandValidator_ValidCommand_Passes()
    {
        var validator = new UpdateGroupCommandValidator();
        var cmd = new UpdateGroupCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            Name: "Updated Name",
            RoomName: "Room 2",
            ScheduleSummary: "Mon/Thu",
            LateGraceMinutes: 20,
            DefaultMonthlyFee: 350.00m,
            ConsecutiveAbsenceThreshold: 4,
            AbsenceAction: AbsenceAction.MarkSuspended,
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateGroupCommandValidator_EmptyName_Fails()
    {
        var validator = new UpdateGroupCommandValidator();
        var cmd = new UpdateGroupCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            Name: "",
            RoomName: null,
            ScheduleSummary: null,
            LateGraceMinutes: 10,
            DefaultMonthlyFee: 100,
            ConsecutiveAbsenceThreshold: null,
            AbsenceAction: AbsenceAction.NotifyOnly,
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateGroupCommand.Name));
    }

    [Fact]
    public void ChangeGroupStatusCommandValidator_ValidCommand_Passes()
    {
        var validator = new ChangeGroupStatusCommandValidator();
        var cmd = new ChangeGroupStatusCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            NewStatus: GroupStatus.Archived,
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EnrollStudentInGroupCommandValidator_ValidCommand_Passes()
    {
        var validator = new EnrollStudentInGroupCommandValidator();
        var cmd = new EnrollStudentInGroupCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EnrollStudentInGroupCommandValidator_EmptyIds_Fails()
    {
        var validator = new EnrollStudentInGroupCommandValidator();
        var cmd = new EnrollStudentInGroupCommand(
            WorkspaceId: Guid.Empty,
            GroupId: Guid.Empty,
            StudentId: Guid.Empty,
            ActorUserId: Guid.Empty);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().Be(4);
    }

    [Fact]
    public void ChangeGroupStudentStatusCommandValidator_ValidCommand_Passes()
    {
        var validator = new ChangeGroupStudentStatusCommandValidator();
        var cmd = new ChangeGroupStudentStatusCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            NewStatus: GroupStudentStatus.Suspended,
            Reason: "Excessive consecutive absences",
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangeGroupStudentStatusCommandValidator_EmptyReason_Fails()
    {
        var validator = new ChangeGroupStudentStatusCommandValidator();
        var cmd = new ChangeGroupStudentStatusCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            NewStatus: GroupStudentStatus.Suspended,
            Reason: "",
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangeGroupStudentStatusCommand.Reason));
    }

    [Fact]
    public void AssignAttendanceDelegateCommandValidator_ValidCommand_Passes()
    {
        var validator = new AssignAttendanceDelegateCommandValidator();
        var cmd = new AssignAttendanceDelegateCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            AssistantUserId: Guid.NewGuid(),
            TeacherUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GrantGroupWorkspaceAccessCommandValidator_ValidCommand_Passes()
    {
        var validator = new GrantGroupWorkspaceAccessCommandValidator();
        var cmd = new GrantGroupWorkspaceAccessCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            GranteeWorkspaceId: Guid.NewGuid(),
            CanViewRoster: true,
            CanRegisterAttendance: true,
            CanEditAttendance: false,
            ExpiresAt: DateTimeOffset.UtcNow.AddMonths(3),
            ActorUserId: Guid.NewGuid(),
            Notes: "Shared center access");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
