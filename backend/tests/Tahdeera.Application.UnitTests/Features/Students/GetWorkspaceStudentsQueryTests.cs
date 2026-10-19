using FluentAssertions;
using Tahdeera.Application.Features.Students.Queries.GetStudentDetails;
using Tahdeera.Application.Features.Students.Queries.GetWorkspaceStudents;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Students;

public class GetWorkspaceStudentsQueryTests
{
    private readonly Guid _workspaceA = Guid.NewGuid();
    private readonly Guid _workspaceB = Guid.NewGuid();

    [Fact]
    public async Task GetWorkspaceStudents_ShouldEnforceWorkspaceIsolation()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var student1 = new Student { Id = Guid.NewGuid(), StudentCode = "STU-2026-00001", FullName = "طالب مركز أ" };
        var student2 = new Student { Id = Guid.NewGuid(), StudentCode = "STU-2026-00002", FullName = "طالب مركز ب" };
        context.Students.AddRange(student1, student2);

        context.WorkspaceStudents.AddRange(
            new WorkspaceStudent { WorkspaceId = _workspaceA, StudentId = student1.Id, Student = student1 },
            new WorkspaceStudent { WorkspaceId = _workspaceB, StudentId = student2.Id, Student = student2 }
        );
        await context.SaveChangesAsync();

        var handler = new GetWorkspaceStudentsQueryHandler(
            new WorkspaceStudentRepository(context),
            new AcademicYearRepository(context),
            new StudentCredentialRepository(context));

        // Act
        var result = await handler.Handle(new GetWorkspaceStudentsQuery(_workspaceA), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(s => s.StudentId == student1.Id);
    }

    [Fact]
    public async Task GetWorkspaceStudents_ShouldFilterBySearchTerm()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var student1 = new Student { Id = Guid.NewGuid(), StudentCode = "STU-2026-11111", FullName = "يوسف أحمد" };
        var student2 = new Student { Id = Guid.NewGuid(), StudentCode = "STU-2026-22222", FullName = "محمد محمود" };
        context.Students.AddRange(student1, student2);

        context.WorkspaceStudents.AddRange(
            new WorkspaceStudent { WorkspaceId = _workspaceA, StudentId = student1.Id, Student = student1, GuardianPhone = "+201011112222" },
            new WorkspaceStudent { WorkspaceId = _workspaceA, StudentId = student2.Id, Student = student2, GuardianPhone = "+201099998888" }
        );
        await context.SaveChangesAsync();

        var handler = new GetWorkspaceStudentsQueryHandler(
            new WorkspaceStudentRepository(context),
            new AcademicYearRepository(context),
            new StudentCredentialRepository(context));

        // Act - search by name
        var resultByName = await handler.Handle(new GetWorkspaceStudentsQuery(_workspaceA, SearchTerm: "يوسف"), CancellationToken.None);
        // Act - search by phone
        var resultByPhone = await handler.Handle(new GetWorkspaceStudentsQuery(_workspaceA, SearchTerm: "99998888"), CancellationToken.None);

        // Assert
        resultByName.Value.Items.Should().ContainSingle(s => s.StudentId == student1.Id);
        resultByPhone.Value.Items.Should().ContainSingle(s => s.StudentId == student2.Id);
    }

    [Fact]
    public async Task GetWorkspaceStudents_ShouldOrderNewestFirstByDefault()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var studentOld = new Student { Id = Guid.NewGuid(), StudentCode = "STU-2026-00001", FullName = "طالب قديم" };
        var studentNew = new Student { Id = Guid.NewGuid(), StudentCode = "STU-2026-00002", FullName = "طالب جديد" };
        context.Students.AddRange(studentOld, studentNew);

        context.WorkspaceStudents.AddRange(
            new WorkspaceStudent { WorkspaceId = _workspaceA, StudentId = studentOld.Id, Student = studentOld, EnrolledAt = DateTimeOffset.UtcNow.AddDays(-10) },
            new WorkspaceStudent { WorkspaceId = _workspaceA, StudentId = studentNew.Id, Student = studentNew, EnrolledAt = DateTimeOffset.UtcNow }
        );
        await context.SaveChangesAsync();

        var handler = new GetWorkspaceStudentsQueryHandler(
            new WorkspaceStudentRepository(context),
            new AcademicYearRepository(context),
            new StudentCredentialRepository(context));

        // Act
        var result = await handler.Handle(new GetWorkspaceStudentsQuery(_workspaceA), CancellationToken.None);

        // Assert
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].StudentId.Should().Be(studentNew.Id);
        result.Value.Items[1].StudentId.Should().Be(studentOld.Id);
    }

    [Fact]
    public async Task GetStudentDetails_WhenNotEnrolled_ShouldReturnNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new GetStudentDetailsQueryHandler(
            new WorkspaceStudentRepository(context),
            new AcademicYearRepository(context),
            new StudentCredentialRepository(context));

        // Act
        var result = await handler.Handle(new GetStudentDetailsQuery(_workspaceA, Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("STUDENT_NOT_FOUND_IN_WORKSPACE");
    }
}
