using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tahdeera.Application.Features.Attendance.Commands.CorrectAttendanceRecord;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;

namespace Tahdeera.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class AttendanceCorrectionGovernanceIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public AttendanceCorrectionGovernanceIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AttendanceCorrection_Enforces7DayWindow_AllowsOwnerOverride_ReconcilesSuspension()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var admin = await db.AppUsers.FirstAsync(u => u.IsPlatformAdmin);
        var workspace = new Workspace { Name = "Governance_WS", Kind = "Center", CreatedByUserId = admin.Id };
        var academicYear = new AcademicYear { Name = "2026/2027", StartsOn = new DateOnly(2026, 9, 1), EndsOn = new DateOnly(2027, 6, 30), IsCurrent = true };
        var grade = await db.Grades.OrderBy(g => g.SortOrder).FirstAsync();

        await db.Workspaces.AddAsync(workspace);
        await db.AcademicYears.AddAsync(academicYear);
        await db.SaveChangesAsync();

        var teacher = new AppUser { Id = Guid.NewGuid(), Username = "gov_teacher", DisplayName = "Gov Teacher", NormalizedEmail = "GOV_TEACHER@TEST.COM", Phone = "+201011116666", IsActive = true };
        await db.AppUsers.AddAsync(teacher);

        var group = Group.Create(workspace.Id, academicYear.Id, grade.Id, "Gov Cohort", teacher.Id, consecutiveAbsenceThreshold: 1, absenceAction: AbsenceAction.MarkSuspended);
        await db.Groups.AddAsync(group);
        await db.SaveChangesAsync();

        var student = new Student { Id = Guid.NewGuid(), FullName = "Gov Student", StudentCode = "STU-00004" };
        await db.Students.AddAsync(student);

        var wsStudent = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            StudentId = student.Id,
            
            Status = WorkspaceStudentStatus.Active
        };
        await db.WorkspaceStudents.AddAsync(wsStudent);

        var enrollment = GroupStudent.Create(group.Id, student.Id, DateTimeOffset.UtcNow);
        // Student was suspended due to reaching absence threshold
        enrollment.Suspend(teacher.Id);
        await db.GroupStudents.AddAsync(enrollment);

        // Session ended 8 days ago (outside the 7-day correction window)
        var oldSession = new Session
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            StartsAt = DateTimeOffset.UtcNow.AddDays(-8).AddHours(-2),
            EndsAt = DateTimeOffset.UtcNow.AddDays(-8),
            LateGraceMinutes = 20,
            Status = SessionStatus.Completed
        };
        await db.Sessions.AddAsync(oldSession);

        var oldAbsentRecord = AttendanceRecord.CreateSystemAbsence(oldSession.Id, student.Id, workspace.Id, DateTimeOffset.UtcNow.AddDays(-8));
        await db.AttendanceRecords.AddAsync(oldAbsentRecord);
        await db.SaveChangesAsync();

        // Act 1: Staff (non-owner) attempts to edit on Day 8
        using var opScope1 = _factory.Services.CreateScope();
        var mediator1 = opScope1.ServiceProvider.GetRequiredService<IMediator>();

        var staffEditResult = await mediator1.Send(new CorrectAttendanceRecordCommand(
            AttendanceRecordId: oldAbsentRecord.Id,
            NewStatus: AttendanceStatus.Excused,
            Reason: "Staff trying to correct after 8 days",
            IsOwner: false
        ));

        // Assert 1: Rejected due to 7-day cutoff
        staffEditResult.IsSuccess.Should().BeFalse();
        staffEditResult.Error.Code.Should().Be(DomainErrors.Attendance.CorrectionWindowExpired.Code);

        // Act 2: Workspace Owner overrides on Day 8
        using var opScope2 = _factory.Services.CreateScope();
        var mediator2 = opScope2.ServiceProvider.GetRequiredService<IMediator>();

        var ownerEditResult = await mediator2.Send(new CorrectAttendanceRecordCommand(
            AttendanceRecordId: oldAbsentRecord.Id,
            NewStatus: AttendanceStatus.Excused,
            Reason: "Owner verified medical certificate after 8 days",
            IsOwner: true
        ));

        // Assert 2: Succeeded via Owner override
        ownerEditResult.IsSuccess.Should().BeTrue();
        ownerEditResult.Value.NewStatus.Should().Be("Excused");
        ownerEditResult.Value.AutoEvaluationLocked.Should().BeTrue();

        // Assert 3: Suspension reconciled & cleared
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

        var reloadedEnrollment = await verifyDb.GroupStudents.FirstAsync(gs => gs.Id == enrollment.Id);
        reloadedEnrollment.Status.Should().Be(GroupStudentStatus.Active);
    }
}
