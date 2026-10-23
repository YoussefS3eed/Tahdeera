using FluentAssertions;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.UnitTests.Entities;

public class ReferenceDataTests
{
    [Fact]
    public void AcademicYear_Initialization_SetsDefaultsAndPreservesProperties()
    {
        // Act
        var year = new AcademicYear
        {
            Name = "2026/2027",
            StartsOn = new DateOnly(2026, 9, 1),
            EndsOn = new DateOnly(2027, 6, 30),
            IsCurrent = true
        };

        // Assert
        year.Id.Should().NotBeEmpty();
        year.Name.Should().Be("2026/2027");
        year.StartsOn.Should().Be(new DateOnly(2026, 9, 1));
        year.EndsOn.Should().Be(new DateOnly(2027, 6, 30));
        year.IsCurrent.Should().BeTrue();
        year.AcademicHistories.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Grade_Initialization_SetsDefaultsAndPreservesProperties()
    {
        // Act
        var grade = new Grade
        {
            Id = 3,
            Code = "G3_SEC",
            NameAr = "الصف الثالث الثانوي",
            NameEn = "Third Secondary",
            SortOrder = 3,
            IsActive = true
        };

        // Assert
        grade.Id.Should().Be(3);
        grade.Code.Should().Be("G3_SEC");
        grade.NameAr.Should().Be("الصف الثالث الثانوي");
        grade.NameEn.Should().Be("Third Secondary");
        grade.SortOrder.Should().Be(3);
        grade.IsActive.Should().BeTrue();
        grade.AcademicHistories.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Subject_Initialization_SetsDefaultsAndPreservesProperties()
    {
        // Act
        var subject = new Subject
        {
            Code = "PHYS_AR",
            NameAr = "الفيزياء",
            NameEn = "Physics",
            IsActive = true
        };

        // Assert
        subject.Id.Should().NotBeEmpty();
        subject.Code.Should().Be("PHYS_AR");
        subject.NameAr.Should().Be("الفيزياء");
        subject.NameEn.Should().Be("Physics");
        subject.IsActive.Should().BeTrue();
    }

    [Fact]
    public void StudentAcademicHistory_Initialization_LinksForeignKeysCorrectly()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var yearId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const short gradeId = 12;

        // Act
        var history = new StudentAcademicHistory
        {
            StudentId = studentId,
            AcademicYearId = yearId,
            GradeId = gradeId,
            RecordedByUserId = userId,
            Notes = "Enrolled in Thanaweya Amma batch"
        };

        // Assert
        history.Id.Should().NotBeEmpty();
        history.StudentId.Should().Be(studentId);
        history.AcademicYearId.Should().Be(yearId);
        history.GradeId.Should().Be(gradeId);
        history.RecordedByUserId.Should().Be(userId);
        history.Notes.Should().Be("Enrolled in Thanaweya Amma batch");
        history.RecordedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
