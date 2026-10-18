using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class MilestoneB2FoundationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public MilestoneB2FoundationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MigrationM2_AppliesCleanly_AgainstPostgreSql18Container()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        // Act
        var tables = await db.Database.SqlQueryRaw<string>(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'"
        ).ToListAsync();

        // Assert: 7 Milestone B2 tables must exist in public schema
        tables.Should().Contain("grades");
        tables.Should().Contain("academic_years");
        tables.Should().Contain("subjects");
        tables.Should().Contain("students");
        tables.Should().Contain("student_academic_history");
        tables.Should().Contain("workspace_students");
        tables.Should().Contain("student_credentials");
    }

    [Fact]
    public async Task GradeSeeder_Populates12EgyptianGrades_SortedCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        // Act
        var grades = await db.Grades.OrderBy(g => g.SortOrder).ToListAsync();

        // Assert: exactly 12 Egyptian Ministry grades
        grades.Should().HaveCount(12);
        grades.Select(g => g.SortOrder).Should().Equal(Enumerable.Range(1, 12));

        // Primary
        grades[0].Code.Should().Be("PRI_1");
        grades[0].NameAr.Should().Be("الصف الأول الابتدائي");
        grades[0].NameEn.Should().Be("Primary 1");

        grades[5].Code.Should().Be("PRI_6");
        grades[5].NameAr.Should().Be("الصف السادس الابتدائي");
        grades[5].NameEn.Should().Be("Primary 6");

        // Preparatory
        grades[6].Code.Should().Be("PREP_1");
        grades[6].NameAr.Should().Be("الصف الأول الإعدادي");
        grades[6].NameEn.Should().Be("Preparatory 1");

        grades[8].Code.Should().Be("PREP_3");
        grades[8].NameAr.Should().Be("الصف الثالث الإعدادي");
        grades[8].NameEn.Should().Be("Preparatory 3");

        // Secondary
        grades[9].Code.Should().Be("SEC_1");
        grades[9].NameAr.Should().Be("الصف الأول الثانوي");
        grades[9].NameEn.Should().Be("Secondary 1");

        grades[11].Code.Should().Be("SEC_3");
        grades[11].NameAr.Should().Be("الصف الثالث الثانوي");
        grades[11].NameEn.Should().Be("Secondary 3");
    }

    [Fact]
    public async Task AcademicYears_Table_RemainsUnseeded()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        // Act
        var count = await db.AcademicYears.CountAsync();

        // Assert: academic_years is strictly managed by platform Super Admin
        count.Should().Be(0);
    }

    [Fact]
    public async Task SingleActiveNfcConstraint_RejectsSecondActiveNfcCard()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var adminUser = await db.AppUsers.FirstAsync(u => u.IsPlatformAdmin);
        var workspace = new Workspace
        {
            Name = "NFC Constraint Center",
            Kind = "Center",
            CreatedByUserId = adminUser.Id
        };
        var student = new Student
        {
            StudentCode = "STU-2026-NFC01",
            FullName = "NFC Student Test"
        };
        await db.Workspaces.AddAsync(workspace);
        await db.Students.AddAsync(student);
        await db.SaveChangesAsync();

        var nfc1 = new StudentCredential
        {
            StudentId = student.Id,
            WorkspaceId = workspace.Id,
            CredentialType = CredentialType.Nfc,
            TokenHash = new byte[] { 0x01, 0x02, 0x03, 0x04 },
            IssuedByUserId = adminUser.Id,
            RevokedAt = null
        };
        await db.StudentCredentials.AddAsync(nfc1);
        await db.SaveChangesAsync();

        // Act: Attempt to add second active NFC card for the same student & workspace
        var nfc2 = new StudentCredential
        {
            StudentId = student.Id,
            WorkspaceId = workspace.Id,
            CredentialType = CredentialType.Nfc,
            TokenHash = new byte[] { 0x05, 0x06, 0x07, 0x08 },
            IssuedByUserId = adminUser.Id,
            RevokedAt = null
        };
        await db.StudentCredentials.AddAsync(nfc2);

        // Assert: Must violate partial unique index ux_one_active_nfc_per_student_workspace
        var act = () => db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SingleCurrentAcademicYearConstraint_RejectsSecondCurrentYear()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var year1 = new AcademicYear
        {
            Name = "2026/2027",
            StartsOn = new DateOnly(2026, 9, 1),
            EndsOn = new DateOnly(2027, 6, 30),
            IsCurrent = true
        };
        await db.AcademicYears.AddAsync(year1);
        await db.SaveChangesAsync();

        // Act: Attempt to add a second current academic year
        var year2 = new AcademicYear
        {
            Name = "2027/2028",
            StartsOn = new DateOnly(2027, 9, 1),
            EndsOn = new DateOnly(2028, 6, 30),
            IsCurrent = true
        };
        await db.AcademicYears.AddAsync(year2);

        // Assert: Must violate partial unique index ux_one_current_academic_year
        var act = () => db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SiblingPhoneSharing_InsertsSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var adminUser = await db.AppUsers.FirstAsync(u => u.IsPlatformAdmin);
        var workspace = new Workspace
        {
            Name = "Sibling Shared Phone Center",
            Kind = "Center",
            CreatedByUserId = adminUser.Id
        };
        var student1 = new Student
        {
            StudentCode = "STU-2026-SIB01",
            FullName = "First Sibling"
        };
        var student2 = new Student
        {
            StudentCode = "STU-2026-SIB02",
            FullName = "Second Sibling"
        };
        await db.Workspaces.AddAsync(workspace);
        await db.Students.AddRangeAsync(student1, student2);
        await db.SaveChangesAsync();

        const string sharedGuardianPhone = "01099998888";

        var wsStudent1 = new WorkspaceStudent
        {
            WorkspaceId = workspace.Id,
            StudentId = student1.Id,
            GuardianName = "Shared Parent",
            GuardianPhone = sharedGuardianPhone
        };
        var wsStudent2 = new WorkspaceStudent
        {
            WorkspaceId = workspace.Id,
            StudentId = student2.Id,
            GuardianName = "Shared Parent",
            GuardianPhone = sharedGuardianPhone
        };

        // Act
        await db.WorkspaceStudents.AddRangeAsync(wsStudent1, wsStudent2);
        var act = () => db.SaveChangesAsync();

        // Assert: Must succeed without constraint violation
        await act.Should().NotThrowAsync();

        var sharedCount = await db.WorkspaceStudents.CountAsync(ws => ws.GuardianPhone == sharedGuardianPhone);
        sharedCount.Should().Be(2);
    }
}
