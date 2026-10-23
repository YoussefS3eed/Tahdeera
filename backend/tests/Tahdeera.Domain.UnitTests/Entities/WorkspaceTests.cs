using FluentAssertions;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.UnitTests.Entities;

public class WorkspaceTests
{
    [Fact]
    public void Workspace_DefaultInitialization_SetsDefaultEgyptianConfig()
    {
        // Act
        var workspace = new Workspace();

        // Assert
        workspace.Id.Should().NotBeEmpty();
        workspace.Name.Should().BeEmpty();
        workspace.Kind.Should().Be("Teacher");
        workspace.TimezoneId.Should().Be("Africa/Cairo");
        workspace.DefaultLateGraceMinutes.Should().Be(20);
        workspace.AutoAbsenceBufferMinutes.Should().Be(15);
        workspace.WeeklyHolidayDays.Should().Equal(5); // Friday
        workspace.IsActive.Should().BeTrue();
        workspace.Members.Should().NotBeNull().And.BeEmpty();
        workspace.Subscriptions.Should().NotBeNull().And.BeEmpty();
        workspace.Groups.Should().NotBeNull().And.BeEmpty();
        workspace.WorkspaceAccesses.Should().NotBeNull().And.BeEmpty();
        workspace.AttendanceRecords.Should().NotBeNull().And.BeEmpty();
        workspace.OutboxMessages.Should().NotBeNull().And.BeEmpty();
        workspace.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        workspace.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Workspace_PropertyAssignment_PreservesValues()
    {
        // Arrange
        var creatorUserId = Guid.NewGuid();

        // Act
        var workspace = new Workspace
        {
            Name = "مجموعة النخبة للفيزياء",
            Kind = "Center",
            TimezoneId = "Africa/Cairo",
            DefaultLateGraceMinutes = 15,
            AutoAbsenceBufferMinutes = 30,
            WeeklyHolidayDays = [5, 6], // Friday and Saturday
            ContactPhone = "+201012345678",
            CityAddress = "مدينة نصر، القاهرة",
            IsActive = true,
            CreatedByUserId = creatorUserId
        };

        // Assert
        workspace.Name.Should().Be("مجموعة النخبة للفيزياء");
        workspace.Kind.Should().Be("Center");
        workspace.DefaultLateGraceMinutes.Should().Be(15);
        workspace.AutoAbsenceBufferMinutes.Should().Be(30);
        workspace.WeeklyHolidayDays.Should().Equal(5, 6);
        workspace.ContactPhone.Should().Be("+201012345678");
        workspace.CityAddress.Should().Be("مدينة نصر، القاهرة");
        workspace.CreatedByUserId.Should().Be(creatorUserId);
    }
}
