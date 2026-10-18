using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.UnitTests.Entities;

public class WorkspaceStudentTests
{
    [Fact]
    public void SetStatus_UpdatesStatusAndTimestamp_PreservesEnrolledAt()
    {
        // Arrange
        var initialEnrolledAt = DateTimeOffset.UtcNow.AddDays(-30);
        var initialUpdatedAt = DateTimeOffset.UtcNow.AddDays(-10);
        var student = new WorkspaceStudent
        {
            EnrolledAt = initialEnrolledAt,
            UpdatedAt = initialUpdatedAt,
            Status = WorkspaceStudentStatus.Active
        };

        // Act
        student.SetStatus(WorkspaceStudentStatus.Blocked);

        // Assert
        student.Status.Should().Be(WorkspaceStudentStatus.Blocked);
        student.EnrolledAt.Should().Be(initialEnrolledAt);
        student.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        student.UpdatedAt.Should().BeAfter(initialUpdatedAt);
    }

    [Fact]
    public void UpdateContactInfo_StampsWhatsappOptInAt_WhenOptedIn()
    {
        // Arrange
        var student = new WorkspaceStudent
        {
            WhatsappOptIn = false,
            WhatsappOptInAt = null
        };

        // Act
        student.UpdateContactInfo("01012345678", "Guardian Name", "01112345678", whatsappOptIn: true, "A-12", "Some notes");

        // Assert
        student.WhatsappOptIn.Should().BeTrue();
        student.WhatsappOptInAt.Should().NotBeNull();
        student.WhatsappOptInAt!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        student.StudentPhone.Should().Be("01012345678");
        student.GuardianName.Should().Be("Guardian Name");
        student.GuardianPhone.Should().Be("01112345678");
        student.SeatNumber.Should().Be("A-12");
        student.LocalNotes.Should().Be("Some notes");
    }

    [Fact]
    public void UpdateContactInfo_ClearsWhatsappOptInAt_WhenRevoked()
    {
        // Arrange
        var initialOptInAt = DateTimeOffset.UtcNow.AddDays(-5);
        var student = new WorkspaceStudent
        {
            WhatsappOptIn = true,
            WhatsappOptInAt = initialOptInAt
        };

        // Act
        student.UpdateContactInfo("01012345678", "Guardian Name", "01112345678", whatsappOptIn: false, "A-12", "Some notes");

        // Assert
        student.WhatsappOptIn.Should().BeFalse();
        student.WhatsappOptInAt.Should().BeNull();
    }
}
