using FluentAssertions;
using Tahdeera.Application.Features.Students.Queries.GetStudent360Profile;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Students;

public class GetStudent360ProfileQueryTests
{
    private readonly Guid _workspaceA = Guid.NewGuid();
    private readonly Guid _workspaceB = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();

    [Fact]
    public async Task GetStudent360Profile_ShouldReturnCompleteComposite_WithZeroDataLeakage()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var grade = new Grade { Id = 10, Code = "SEC_1", NameAr = "الصف الأول الثانوي", NameEn = "Secondary 1" };
        var academicYear = new AcademicYear { Id = Guid.NewGuid(), Name = "2026/2027", StartsOn = new DateOnly(2026, 9, 1), EndsOn = new DateOnly(2027, 6, 30), IsCurrent = true };
        context.Grades.Add(grade);
        context.AcademicYears.Add(academicYear);

        var student = new Student
        {
            Id = _studentId,
            StudentCode = "STU-2026-00001",
            FullName = "أحمد خالد محمود",
            IsActive = true
        };
        context.Students.Add(student);

        // Academic history
        context.StudentAcademicHistories.Add(new StudentAcademicHistory
        {
            StudentId = _studentId,
            AcademicYearId = academicYear.Id,
            AcademicYear = academicYear,
            GradeId = grade.Id,
            Grade = grade,
            RecordedAt = DateTimeOffset.UtcNow
        });

        // Workspace A enrollment (Center A)
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceA,
            StudentId = _studentId,
            Status = WorkspaceStudentStatus.Active,
            GuardianName = "خالد محمود - مركز أ",
            GuardianPhone = "+201011111111",
            LocalNotes = "ملاحظات مركز أ السرية"
        });

        // Workspace B enrollment (Center B) - should NOT leak into Workspace A query
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceB,
            StudentId = _studentId,
            Status = WorkspaceStudentStatus.Active,
            GuardianName = "خالد محمود - مركز ب",
            GuardianPhone = "+201022222222",
            LocalNotes = "ملاحظات مركز ب الحصرية"
        });

        // Credential in Workspace A
        context.StudentCredentials.Add(new StudentCredential
        {
            StudentId = _studentId,
            WorkspaceId = _workspaceA,
            CredentialType = CredentialType.Nfc,
            CardUidDisplay = "Card #1234",
            TokenHash = new byte[] { 1, 2, 3 }
        });

        // Credential in Workspace B (should not appear in Workspace A)
        context.StudentCredentials.Add(new StudentCredential
        {
            StudentId = _studentId,
            WorkspaceId = _workspaceB,
            CredentialType = CredentialType.Nfc,
            CardUidDisplay = "Card #B999",
            TokenHash = new byte[] { 4, 5, 6 }
        });

        await context.SaveChangesAsync();

        var handler = new GetStudent360ProfileQueryHandler(
            new WorkspaceStudentRepository(context),
            new StudentCredentialRepository(context),
            new AuditLogRepository(context));

        // Act
        var result = await handler.Handle(new GetStudent360ProfileQuery(_workspaceA, _studentId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var profile = result.Value;

        profile.StudentId.Should().Be(_studentId);
        profile.StudentCode.Should().Be("STU-2026-00001");
        profile.FullName.Should().Be("أحمد خالد محمود");

        // Center A enrollment details
        profile.WorkspaceEnrollment.GuardianPhone.Should().Be("+201011111111");
        profile.WorkspaceEnrollment.LocalNotes.Should().Be("ملاحظات مركز أ السرية");

        // Credentials isolated to Center A
        profile.Credentials.Should().ContainSingle();
        profile.Credentials[0].CardUidDisplay.Should().Be("Card #1234");

        // Academic history
        profile.AcademicHistory.Should().ContainSingle();
        profile.AcademicHistory[0].GradeNameAr.Should().Be("الصف الأول الثانوي");

        // Safe defaults for future modules
        profile.Groups.Should().BeEmpty();
        profile.AttendanceSummary.TotalSessions.Should().Be(0);
        profile.AttendanceSummary.AttendancePercentage.Should().Be(0.0);
        profile.FinancialSummary.OutstandingBalance.Should().Be(0m);
    }

    [Fact]
    public async Task GetStudent360Profile_WhenNotEnrolled_ShouldReturnNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new GetStudent360ProfileQueryHandler(
            new WorkspaceStudentRepository(context),
            new StudentCredentialRepository(context),
            new AuditLogRepository(context));

        // Act
        var result = await handler.Handle(new GetStudent360ProfileQuery(_workspaceA, Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("STUDENT_NOT_FOUND_IN_WORKSPACE");
    }
}
