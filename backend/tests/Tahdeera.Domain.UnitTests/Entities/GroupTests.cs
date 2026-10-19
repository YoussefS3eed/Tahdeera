using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.UnitTests.Entities;

public class GroupTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly short _gradeId = 10;
    private readonly Guid _teacherUserId = Guid.NewGuid();

    [Theory]
    [InlineData(-1)]
    [InlineData(1441)]
    public void Group_Create_Throws_When_LateGraceMinutes_OutOfRange(int invalidMinutes)
    {
        var act = () => Group.Create(
            _workspaceId, _academicYearId, _gradeId, "Physics A", _teacherUserId,
            lateGraceMinutes: invalidMinutes);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("lateGraceMinutes");
    }

    [Fact]
    public void Group_Create_Throws_When_DefaultMonthlyFee_Negative()
    {
        var act = () => Group.Create(
            _workspaceId, _academicYearId, _gradeId, "Physics A", _teacherUserId,
            defaultMonthlyFee: -0.01m);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("defaultMonthlyFee");
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)51)]
    public void Group_Create_Throws_When_AbsenceThreshold_OutOfRange(short invalidThreshold)
    {
        var act = () => Group.Create(
            _workspaceId, _academicYearId, _gradeId, "Physics A", _teacherUserId,
            consecutiveAbsenceThreshold: invalidThreshold);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("consecutiveAbsenceThreshold");
    }

    [Fact]
    public void Group_Create_Initializes_With_Active_Status_And_Default_Values()
    {
        var group = Group.Create(
            _workspaceId, _academicYearId, _gradeId, " Chemistry Group 1 ", _teacherUserId);

        group.Id.Should().NotBeEmpty();
        group.Name.Should().Be("Chemistry Group 1");
        group.Status.Should().Be(GroupStatus.Active);
        group.LateGraceMinutes.Should().Be(20);
        group.DefaultMonthlyFee.Should().Be(0.00m);
        group.AbsenceAction.Should().Be(AbsenceAction.NotifyOnly);
        group.ConsecutiveAbsenceThreshold.Should().BeNull();
        group.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        group.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Group_UpdateDetails_Trims_Name_And_Touches_UpdatedAt()
    {
        var group = Group.Create(
            _workspaceId, _academicYearId, _gradeId, "Math 101", _teacherUserId);

        var originalUpdatedAt = group.UpdatedAt;

        group.UpdateDetails(" Advanced Math ", "Room 302", "Sun/Tue 4PM", null, Guid.NewGuid());

        group.Name.Should().Be("Advanced Math");
        group.RoomName.Should().Be("Room 302");
        group.ScheduleSummary.Should().Be("Sun/Tue 4PM");
        group.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void Group_UpdateAbsencePolicy_Updates_Threshold_And_Action()
    {
        var group = Group.Create(
            _workspaceId, _academicYearId, _gradeId, "Biology", _teacherUserId);

        group.UpdateAbsencePolicy(3, AbsenceAction.MarkSuspended, Guid.NewGuid());

        group.ConsecutiveAbsenceThreshold.Should().Be(3);
        group.AbsenceAction.Should().Be(AbsenceAction.MarkSuspended);
    }

    [Fact]
    public void Group_SetStatus_Updates_Status_And_Touches_UpdatedAt()
    {
        var group = Group.Create(
            _workspaceId, _academicYearId, _gradeId, "History", _teacherUserId);

        var originalUpdatedAt = group.UpdatedAt;

        group.SetStatus(GroupStatus.Paused, Guid.NewGuid());

        group.Status.Should().Be(GroupStatus.Paused);
        group.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }
}
