using FluentAssertions;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.UnitTests.Entities;

public class WorkspaceReceiptSequenceTests
{
    [Fact]
    public void Constructor_WithDefaultNextSequence_SetsProperties()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var yearMonth = 202610;

        // Act
        var sequence = new WorkspaceReceiptSequence(workspaceId, yearMonth);

        // Assert
        sequence.WorkspaceId.Should().Be(workspaceId);
        sequence.YearMonth.Should().Be(yearMonth);
        sequence.NextSequence.Should().Be(1);
        sequence.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WithCustomNextSequence_SetsSpecifiedSequence()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var yearMonth = 202611;

        // Act
        var sequence = new WorkspaceReceiptSequence(workspaceId, yearMonth, nextSequence: 42);

        // Assert
        sequence.WorkspaceId.Should().Be(workspaceId);
        sequence.YearMonth.Should().Be(yearMonth);
        sequence.NextSequence.Should().Be(42);
    }
}
