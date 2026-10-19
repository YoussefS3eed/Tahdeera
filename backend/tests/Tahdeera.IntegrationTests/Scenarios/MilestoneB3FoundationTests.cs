using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class MilestoneB3FoundationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public MilestoneB3FoundationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Workspace Workspace, AcademicYear AcademicYear, Grade Grade, AppUser Teacher, AppUser Assistant)> SeedPrerequisitesAsync(TahdeeraDbContext db)
    {
        var adminUser = await db.AppUsers.FirstAsync(u => u.IsPlatformAdmin);

        var teacher = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = $"teacher_{Guid.NewGuid():N}",
            DisplayName = "Test Teacher",
            NormalizedEmail = $"TEACHER_{Guid.NewGuid():N}@TEST.COM",
            Phone = "+201011112222",
            IsActive = true
        };

        var assistant = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = $"assistant_{Guid.NewGuid():N}",
            DisplayName = "Test Assistant",
            NormalizedEmail = $"ASSISTANT_{Guid.NewGuid():N}@TEST.COM",
            Phone = "+201033334444",
            IsActive = true
        };

        var workspace = new Workspace
        {
            Name = $"Workspace_{Guid.NewGuid():N}",
            Kind = "Teacher",
            CreatedByUserId = adminUser.Id
        };

        var academicYear = new AcademicYear
        {
            Name = "2026/2027",
            StartsOn = new DateOnly(2026, 9, 1),
            EndsOn = new DateOnly(2027, 6, 30),
            IsCurrent = true
        };

        await db.AppUsers.AddRangeAsync(teacher, assistant);
        await db.Workspaces.AddAsync(workspace);
        await db.AcademicYears.AddAsync(academicYear);
        await db.SaveChangesAsync();

        var grade = await db.Grades.OrderBy(g => g.SortOrder).FirstAsync();

        return (workspace, academicYear, grade, teacher, assistant);
    }

    [Fact]
    public async Task Database_Applies_Migration_M3_Without_Errors()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        // Act
        var tables = await db.Database.SqlQueryRaw<string>(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'"
        ).ToListAsync();

        // Assert: All 9 Milestone B3 tables must exist
        tables.Should().Contain("groups");
        tables.Should().Contain("group_students");
        tables.Should().Contain("group_student_status_history");
        tables.Should().Contain("group_workspace_access");
        tables.Should().Contain("group_attendance_delegations");
        tables.Should().Contain("sessions");
        tables.Should().Contain("attendance_records");
        tables.Should().Contain("attendance_history");
        tables.Should().Contain("message_outbox");
    }

    [Fact]
    public async Task Single_Active_Attendance_Delegate_Constraint_Enforced()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var (workspace, academicYear, grade, teacher, assistant1) = await SeedPrerequisitesAsync(db);

        var assistant2 = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = $"assistant2_{Guid.NewGuid():N}",
            DisplayName = "Second Assistant",
            NormalizedEmail = $"ASSISTANT2_{Guid.NewGuid():N}@TEST.COM",
            Phone = "+201055556666",
            IsActive = true
        };
        await db.AppUsers.AddAsync(assistant2);

        var group = Group.Create(workspace.Id, academicYear.Id, grade.Id, "Physics Senior", teacher.Id);
        await db.Groups.AddAsync(group);
        await db.SaveChangesAsync();

        var delegation1 = GroupAttendanceDelegation.Create(group.Id, assistant1.Id, teacher.Id);
        await db.GroupAttendanceDelegations.AddAsync(delegation1);
        await db.SaveChangesAsync();

        // Act: Attempt to assign second active delegate for the same group
        var delegation2 = GroupAttendanceDelegation.Create(group.Id, assistant2.Id, teacher.Id);
        await db.GroupAttendanceDelegations.AddAsync(delegation2);

        // Assert: Must violate ux_one_active_attendance_delegate_per_group
        var act = () => db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Revoking_Attendance_Delegation_Allows_New_Active_Delegate()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var (workspace, academicYear, grade, teacher, assistant1) = await SeedPrerequisitesAsync(db);

        var assistant2 = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = $"assistant2_{Guid.NewGuid():N}",
            DisplayName = "Second Assistant",
            NormalizedEmail = $"ASSISTANT2_{Guid.NewGuid():N}@TEST.COM",
            Phone = "+201077778888",
            IsActive = true
        };
        await db.AppUsers.AddAsync(assistant2);

        var group = Group.Create(workspace.Id, academicYear.Id, grade.Id, "Chemistry Senior", teacher.Id);
        await db.Groups.AddAsync(group);
        await db.SaveChangesAsync();

        var delegation1 = GroupAttendanceDelegation.Create(group.Id, assistant1.Id, teacher.Id);
        await db.GroupAttendanceDelegations.AddAsync(delegation1);
        await db.SaveChangesAsync();

        // Revoke first delegation
        delegation1.Revoke(teacher.Id, "Assistant rotated", DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();

        // Act: Assign second active delegate after revocation
        var delegation2 = GroupAttendanceDelegation.Create(group.Id, assistant2.Id, teacher.Id);
        await db.GroupAttendanceDelegations.AddAsync(delegation2);
        var act = () => db.SaveChangesAsync();

        // Assert: Must succeed due to partial filter WHERE revoked_at IS NULL
        await act.Should().NotThrowAsync();

        var count = await db.GroupAttendanceDelegations.CountAsync(d => d.GroupId == group.Id);
        count.Should().Be(2);
    }

    [Fact]
    public async Task Unique_Group_Name_Per_Workspace_And_Year_Enforced()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var (workspace, academicYear, grade, teacher, _) = await SeedPrerequisitesAsync(db);

        var group1 = Group.Create(workspace.Id, academicYear.Id, grade.Id, "Math Cohort A", teacher.Id);
        await db.Groups.AddAsync(group1);
        await db.SaveChangesAsync();

        // Act: Attempt to insert duplicate name for same workspace and academic year
        var group2 = Group.Create(workspace.Id, academicYear.Id, grade.Id, "Math Cohort A", teacher.Id);
        await db.Groups.AddAsync(group2);

        // Assert
        var act = () => db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Session_EndsAt_Check_Constraint_Enforced()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var (workspace, academicYear, grade, teacher, _) = await SeedPrerequisitesAsync(db);

        var group = Group.Create(workspace.Id, academicYear.Id, grade.Id, "Biology Cohort", teacher.Id);
        await db.Groups.AddAsync(group);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        // Inverted time range: ends_at <= starts_at
        var invalidSession = new Session
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            StartsAt = now.AddHours(2),
            EndsAt = now.AddHours(1),
            LateGraceMinutes = 20,
            Status = SessionStatus.Scheduled
        };

        await db.Sessions.AddAsync(invalidSession);

        // Assert: Violates CK_sessions_ends_at
        var act = () => db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task MessageOutbox_Idempotency_Key_Unique_Constraint_Enforced()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var (workspace, _, _, _, _) = await SeedPrerequisitesAsync(db);

        const string idempotencyKey = "idemp_test_duplicate_key";

        var outbox1 = new MessageOutbox
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            RecipientPhone = "+201099990000",
            MessageType = "AttendanceCheckIn",
            Payload = "{\"test\":true}",
            IdempotencyKey = idempotencyKey,
            Status = OutboxMessageStatus.Pending
        };
        await db.MessageOutboxes.AddAsync(outbox1);
        await db.SaveChangesAsync();

        var outbox2 = new MessageOutbox
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            RecipientPhone = "+201099990001",
            MessageType = "AttendanceCheckIn",
            Payload = "{\"test\":true}",
            IdempotencyKey = idempotencyKey,
            Status = OutboxMessageStatus.Pending
        };
        await db.MessageOutboxes.AddAsync(outbox2);

        // Assert: Must violate unique index on idempotency_key
        var act = () => db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Attendance_Guest_Makeup_Partial_Index_Queries()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var (workspace, academicYear, grade, teacher, _) = await SeedPrerequisitesAsync(db);

        var groupA = Group.Create(workspace.Id, academicYear.Id, grade.Id, "Cohort Alpha", teacher.Id);
        var groupB = Group.Create(workspace.Id, academicYear.Id, grade.Id, "Cohort Beta", teacher.Id);
        var student = new Student { StudentCode = "STU-MAKEUP-01", FullName = "Makeup Student" };

        await db.Groups.AddRangeAsync(groupA, groupB);
        await db.Students.AddAsync(student);
        await db.SaveChangesAsync();

        var sessionA = Session.Create(groupA.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2));
        var sessionB = Session.Create(groupB.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2));
        await db.Sessions.AddRangeAsync(sessionA, sessionB);
        await db.SaveChangesAsync();

        // Regular attendance in group A
        var regularRecord = AttendanceRecord.CreateCheckIn(sessionA, student.Id, workspace.Id, DateTimeOffset.UtcNow, null);

        // Makeup attendance in group B with HomeGroupId = groupA
        var makeupRecord = AttendanceRecord.RegisterMakeup(sessionB, student.Id, workspace.Id, groupA.Id, DateTimeOffset.UtcNow, null);

        await db.AttendanceRecords.AddRangeAsync(regularRecord, makeupRecord);
        await db.SaveChangesAsync();

        // Act
        var makeupList = await db.AttendanceRecords
            .Where(ar => ar.StudentId == student.Id && ar.IsGuestMakeup)
            .ToListAsync();

        // Assert
        makeupList.Should().HaveCount(1);
        makeupList[0].HomeGroupId.Should().Be(groupA.Id);
        makeupList[0].IsGuestMakeup.Should().BeTrue();
    }
}
