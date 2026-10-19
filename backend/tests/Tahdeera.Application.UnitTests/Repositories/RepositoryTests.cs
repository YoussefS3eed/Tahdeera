using FluentAssertions;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Repositories;

public class RepositoryTests
{
    [Fact]
    public async Task StudentRepository_ShouldPerformLookupsAndIncludeHistories()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var repo = new StudentRepository(context);

        var grade = new Grade { Id = 1, Code = "PRI_1", NameAr = "الأول", NameEn = "P1", SortOrder = 1, IsActive = true };
        var year = new AcademicYear { Id = Guid.NewGuid(), Name = "2026/2027", StartsOn = new DateOnly(2026, 9, 1), EndsOn = new DateOnly(2027, 6, 30), IsCurrent = true };
        context.Grades.Add(grade);
        context.AcademicYears.Add(year);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            StudentCode = "STU-2026-00001",
            FullName = "أحمد خالد",
            IsActive = true
        };
        student.AcademicHistories.Add(new StudentAcademicHistory
        {
            AcademicYearId = year.Id,
            GradeId = grade.Id,
            RecordedAt = DateTimeOffset.UtcNow
        });
        await repo.AddAsync(student);
        await context.SaveChangesAsync();

        // Act & Assert
        var exists = await repo.ExistsByCodeAsync("stu-2026-00001");
        exists.Should().BeTrue();

        var byCode = await repo.GetByCodeAsync("STU-2026-00001");
        byCode.Should().NotBeNull();
        byCode!.FullName.Should().Be("أحمد خالد");

        var fullGraph = await repo.GetWithAcademicHistoryAndEnrollmentsAsync(student.Id);
        fullGraph.Should().NotBeNull();
        fullGraph!.AcademicHistories.Should().HaveCount(1);
    }

    [Fact]
    public async Task WorkspaceStudentRepository_ShouldExecuteDomainQueries()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var repo = new WorkspaceStudentRepository(context);
        var workspaceId = Guid.NewGuid();
        var student1Id = Guid.NewGuid();
        var student2Id = Guid.NewGuid();

        var student1 = new Student { Id = student1Id, StudentCode = "STU-1", FullName = "طالب 1" };
        var student2 = new Student { Id = student2Id, StudentCode = "STU-2", FullName = "طالب 2" };
        context.Students.AddRange(student1, student2);

        var ws1 = new WorkspaceStudent
        {
            WorkspaceId = workspaceId,
            StudentId = student1Id,
            Status = WorkspaceStudentStatus.Active,
            GuardianPhone = "+201011112222",
            Student = student1
        };
        var ws2 = new WorkspaceStudent
        {
            WorkspaceId = workspaceId,
            StudentId = student2Id,
            Status = WorkspaceStudentStatus.Inactive,
            GuardianPhone = "+201033334444",
            Student = student2
        };

        await repo.AddRangeAsync(new[] { ws1, ws2 });
        await context.SaveChangesAsync();

        // Act & Assert
        var isEnrolled = await repo.IsStudentEnrolledAsync(workspaceId, student1Id);
        isEnrolled.Should().BeTrue();

        var notEnrolled = await repo.IsStudentEnrolledAsync(workspaceId, Guid.NewGuid());
        notEnrolled.Should().BeFalse();

        var activeCount = await repo.GetActiveStudentCountAsync(workspaceId);
        activeCount.Should().Be(1);

        var phoneExists = await repo.GuardianPhoneExistsInWorkspaceAsync(workspaceId, "+201011112222");
        phoneExists.Should().BeTrue();

        var phoneExcluded = await repo.GuardianPhoneExistsInWorkspaceAsync(workspaceId, "+201011112222", student1Id);
        phoneExcluded.Should().BeFalse();

        var (items, total) = await repo.GetWorkspaceRosterAsync(
            workspaceId, searchTerm: "طالب 1", gradeId: null, academicYearId: null, status: null,
            pageNumber: 1, pageSize: 10, sortBy: "name", sortDirection: "asc");
        total.Should().Be(1);
        items[0].StudentId.Should().Be(student1Id);
    }

    [Fact]
    public async Task StudentCredentialRepository_ShouldManageLifecycleLookups()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var repo = new StudentCredentialRepository(context);
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var tokenHash = new byte[] { 10, 20, 30 };

        var user = new AppUser { Id = Guid.NewGuid(), DisplayName = "Officer" };
        context.AppUsers.Add(user);

        var student = new Student { Id = studentId, StudentCode = "STU-2026-NFC01", FullName = "NFC Student" };
        context.Students.Add(student);

        var activeNfc = new StudentCredential
        {
            WorkspaceId = workspaceId,
            StudentId = studentId,
            Student = student,
            CredentialType = CredentialType.Nfc,
            TokenHash = tokenHash,
            CardUidDisplay = "NFC-001",
            IssuedByUserId = user.Id,
            IssuedByUser = user,
            RevokedAt = null
        };
        await repo.AddAsync(activeNfc);
        await context.SaveChangesAsync();

        // Act & Assert
        var byToken = await repo.GetActiveByTokenHashAsync(tokenHash);
        byToken.Should().NotBeNull();
        byToken!.CardUidDisplay.Should().Be("NFC-001");

        var tokenExists = await repo.TokenHashExistsAsync(tokenHash);
        tokenExists.Should().BeTrue();

        var activeCard = await repo.GetActiveCardForStudentAsync(workspaceId, studentId, CredentialType.Nfc);
        activeCard.Should().NotBeNull();

        var hasActive = await repo.HasActiveCredentialAsync(workspaceId, studentId);
        hasActive.Should().BeTrue();

        var creds = await repo.GetCredentialsByStudentAsync(workspaceId, studentId, includeUserDetails: true);
        creds.Should().HaveCount(1);
        creds[0].IssuedByUser.Should().NotBeNull();

        var activeSet = await repo.GetStudentIdsWithActiveCardsAsync(workspaceId, new[] { studentId, Guid.NewGuid() });
        activeSet.Should().Contain(studentId);
    }

    [Fact]
    public async Task GradeAndAcademicYearAndAuditRepositories_ShouldReturnCorrectData()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var gradeRepo = new GradeRepository(context);
        var yearRepo = new AcademicYearRepository(context);
        var auditRepo = new AuditLogRepository(context);

        var grade1 = new Grade { Id = 1, Code = "G1", NameAr = "صف 1", NameEn = "G1", SortOrder = 1, IsActive = true };
        var grade2 = new Grade { Id = 2, Code = "G2", NameAr = "صف 2", NameEn = "G2", SortOrder = 2, IsActive = false };
        await gradeRepo.AddRangeAsync(new[] { grade1, grade2 });

        var year1 = new AcademicYear { Id = Guid.NewGuid(), Name = "2025/2026", StartsOn = new DateOnly(2025, 9, 1), EndsOn = new DateOnly(2026, 6, 30), IsCurrent = false };
        var year2 = new AcademicYear { Id = Guid.NewGuid(), Name = "2026/2027", StartsOn = new DateOnly(2026, 9, 1), EndsOn = new DateOnly(2027, 6, 30), IsCurrent = true };
        await yearRepo.AddRangeAsync(new[] { year1, year2 });

        var workspaceId = Guid.NewGuid();
        var user = new AppUser { Id = Guid.NewGuid(), DisplayName = "Manager" };
        context.AppUsers.Add(user);

        var audit = new AuditLog
        {
            WorkspaceId = workspaceId,
            EntityId = "ENTITY-123",
            Action = "TEST_ACTION",
            OccurredAt = DateTimeOffset.UtcNow,
            ActorUser = user
        };
        await auditRepo.AddAsync(audit);
        await context.SaveChangesAsync();

        // Act & Assert
        var activeGrades = await gradeRepo.GetActiveGradesAsync();
        activeGrades.Should().HaveCount(1);
        activeGrades[0].SortOrder.Should().Be(1);

        var currentYear = await yearRepo.GetCurrentYearAsync();
        currentYear.Should().NotBeNull();
        currentYear!.IsCurrent.Should().BeTrue();

        var allYears = await yearRepo.GetAllOrderedAsync();
        allYears.Should().HaveCount(2);
        allYears[0].StartsOn.Should().Be(new DateOnly(2026, 9, 1));

        var logs = await auditRepo.GetRecentLogsForEntitiesAsync(workspaceId, new[] { "ENTITY-123" });
        logs.Should().HaveCount(1);
        logs[0].ActorUser.Should().NotBeNull();
        logs[0].ActorUser!.DisplayName.Should().Be("Manager");
    }
}
