using FluentAssertions;
using Tahdeera.Application.Features.ReferenceData.Queries.GetAcademicYears;
using Tahdeera.Application.Features.ReferenceData.Queries.GetCurrentAcademicYear;
using Tahdeera.Application.Features.ReferenceData.Queries.GetGrades;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.ReferenceData;

public class ReferenceDataQueryTests
{
    [Fact]
    public async Task GetGradesQuery_ShouldReturnGradesOrderedBySortOrderAsc()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Grades.AddRange(
            new Grade { Id = 2, Code = "G2", NameAr = "الصف الثاني", NameEn = "Grade 2", SortOrder = 2, IsActive = true },
            new Grade { Id = 1, Code = "G1", NameAr = "الصف الأول", NameEn = "Grade 1", SortOrder = 1, IsActive = true },
            new Grade { Id = 3, Code = "G3", NameAr = "غير نشط", NameEn = "Inactive", SortOrder = 3, IsActive = false }
        );
        await context.SaveChangesAsync();

        var handler = new GetGradesQueryHandler(new GradeRepository(context));

        // Act
        var result = await handler.Handle(new GetGradesQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].SortOrder.Should().Be(1);
        result.Value[1].SortOrder.Should().Be(2);
    }

    [Fact]
    public async Task GetAcademicYearsQuery_ShouldReturnYearsOrderedByStartsOnDesc()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.AcademicYears.AddRange(
            new AcademicYear { Name = "2025/2026", StartsOn = new DateOnly(2025, 9, 1), EndsOn = new DateOnly(2026, 6, 30), IsCurrent = false },
            new AcademicYear { Name = "2026/2027", StartsOn = new DateOnly(2026, 9, 1), EndsOn = new DateOnly(2027, 6, 30), IsCurrent = true }
        );
        await context.SaveChangesAsync();

        var handler = new GetAcademicYearsQueryHandler(new AcademicYearRepository(context));

        // Act
        var result = await handler.Handle(new GetAcademicYearsQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].StartsOn.Should().Be(new DateOnly(2026, 9, 1));
        result.Value[1].StartsOn.Should().Be(new DateOnly(2025, 9, 1));
    }

    [Fact]
    public async Task GetCurrentAcademicYearQuery_WhenNoCurrentYearConfigured_ShouldReturnNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new GetCurrentAcademicYearQueryHandler(new AcademicYearRepository(context));

        // Act
        var result = await handler.Handle(new GetCurrentAcademicYearQuery(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ACTIVE_ACADEMIC_YEAR_NOT_CONFIGURED");
    }

    [Fact]
    public async Task GetCurrentAcademicYearQuery_WhenCurrentYearExists_ShouldReturnYear()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.AcademicYears.Add(new AcademicYear
        {
            Name = "2026/2027",
            StartsOn = new DateOnly(2026, 9, 1),
            EndsOn = new DateOnly(2027, 6, 30),
            IsCurrent = true
        });
        await context.SaveChangesAsync();

        var handler = new GetCurrentAcademicYearQueryHandler(new AcademicYearRepository(context));

        // Act
        var result = await handler.Handle(new GetCurrentAcademicYearQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NameAr.Should().Be("2026/2027");
        result.Value.IsCurrent.Should().BeTrue();
    }
}
