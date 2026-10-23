using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.UnitTests.Entities;

public class AuditAndHistoryTests
{
    [Fact]
    public void AuditLog_Initialization_SetsDefaultsAndPreservesProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var log = new AuditLog
        {
            Id = 1001,
            ActorUserId = userId,
            WorkspaceId = workspaceId,
            Action = "STUDENT_CREDENTIAL_REVOKED",
            EntityType = "StudentCredential",
            EntityId = "CARD-12345",
            OccurredAt = now,
            IpAddress = "192.168.1.100",
            Metadata = "{\"reason\":\"Lost card\"}"
        };

        // Assert
        log.Id.Should().Be(1001);
        log.ActorUserId.Should().Be(userId);
        log.WorkspaceId.Should().Be(workspaceId);
        log.Action.Should().Be("STUDENT_CREDENTIAL_REVOKED");
        log.EntityType.Should().Be("StudentCredential");
        log.EntityId.Should().Be("CARD-12345");
        log.OccurredAt.Should().Be(now);
        log.IpAddress.Should().Be("192.168.1.100");
        log.Metadata.Should().Be("{\"reason\":\"Lost card\"}");
    }

    [Fact]
    public void AttendanceHistory_Create_FactoryMethod_TrimsReasonAndSetsDefaults()
    {
        // Arrange
        var recordId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();
        var changedAt = DateTimeOffset.UtcNow;

        // Act
        var history = AttendanceHistory.Create(
            attendanceRecordId: recordId,
            oldStatus: AttendanceStatus.Absent,
            newStatus: AttendanceStatus.Excused,
            oldSource: AttendanceSource.System,
            newSource: AttendanceSource.User,
            changedByUserId: changedBy,
            changedAt: changedAt,
            reason: "  Medical excuse submitted by guardian  ",
            changeKind: AttendanceChangeKind.ManualCorrection,
            metadata: "");

        // Assert
        history.AttendanceRecordId.Should().Be(recordId);
        history.OldStatus.Should().Be(AttendanceStatus.Absent);
        history.NewStatus.Should().Be(AttendanceStatus.Excused);
        history.OldSource.Should().Be(AttendanceSource.System);
        history.NewSource.Should().Be(AttendanceSource.User);
        history.ChangedByUserId.Should().Be(changedBy);
        history.ChangedAt.Should().Be(changedAt);
        history.Reason.Should().Be("Medical excuse submitted by guardian");
        history.ChangeKind.Should().Be(AttendanceChangeKind.ManualCorrection);
        history.Metadata.Should().Be("{}");
    }

    [Fact]
    public void GroupStudentStatusHistory_Record_FactoryMethod_TrimsReasonAndSetsTimestamps()
    {
        // Arrange
        var groupStudentId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();
        var effectiveAt = DateTimeOffset.UtcNow;

        // Act
        var statusHistory = GroupStudentStatusHistory.Record(
            groupStudentId: groupStudentId,
            oldStatus: GroupStudentStatus.Active,
            newStatus: GroupStudentStatus.Suspended,
            effectiveAt: effectiveAt,
            changedByUserId: changedBy,
            reason: "  Repeated unauthorized absences  ");

        // Assert
        statusHistory.GroupStudentId.Should().Be(groupStudentId);
        statusHistory.OldStatus.Should().Be(GroupStudentStatus.Active);
        statusHistory.NewStatus.Should().Be(GroupStudentStatus.Suspended);
        statusHistory.EffectiveAt.Should().Be(effectiveAt);
        statusHistory.ChangedByUserId.Should().Be(changedBy);
        statusHistory.Reason.Should().Be("Repeated unauthorized absences");
        statusHistory.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
