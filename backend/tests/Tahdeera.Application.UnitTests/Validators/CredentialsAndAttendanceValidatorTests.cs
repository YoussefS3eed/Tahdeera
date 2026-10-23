using FluentAssertions;
using Tahdeera.Application.Features.Attendance.Commands.CorrectAttendanceRecord;
using Tahdeera.Application.Features.Attendance.Commands.ManualCheckIn;
using Tahdeera.Application.Features.Attendance.Commands.RegisterGuestMakeupAttendance;
using Tahdeera.Application.Features.Attendance.Commands.ScanCardAttendance;
using Tahdeera.Application.Features.Credentials.Commands.IssueNfcCredential;
using Tahdeera.Application.Features.Credentials.Commands.IssueQrCredential;
using Tahdeera.Application.Features.Credentials.Commands.RevokeCredential;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.UnitTests.Validators;

public class CredentialsAndAttendanceValidatorTests
{
    [Fact]
    public void CorrectAttendanceRecordCommandValidator_ValidCommand_Passes()
    {
        var validator = new CorrectAttendanceRecordCommandValidator();
        var cmd = new CorrectAttendanceRecordCommand(
            AttendanceRecordId: Guid.NewGuid(),
            NewStatus: AttendanceStatus.Present,
            Reason: "تصحيح بسبب عطل جهاز المسح",
            IsOwner: false);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CorrectAttendanceRecordCommandValidator_EmptyReasonOrId_Fails()
    {
        var validator = new CorrectAttendanceRecordCommandValidator();
        var cmd = new CorrectAttendanceRecordCommand(
            AttendanceRecordId: Guid.Empty,
            NewStatus: AttendanceStatus.Present,
            Reason: "");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CorrectAttendanceRecordCommand.AttendanceRecordId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CorrectAttendanceRecordCommand.Reason));
    }

    [Fact]
    public void ManualCheckInCommandValidator_ValidCommand_Passes()
    {
        var validator = new ManualCheckInCommandValidator();
        var cmd = new ManualCheckInCommand(
            SessionId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            RetroactiveReason: "نسي البطاقة الذكية");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ManualCheckInCommandValidator_EmptyIds_Fails()
    {
        var validator = new ManualCheckInCommandValidator();
        var cmd = new ManualCheckInCommand(
            SessionId: Guid.Empty,
            StudentId: Guid.Empty);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ManualCheckInCommand.SessionId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ManualCheckInCommand.StudentId));
    }

    [Fact]
    public void RegisterGuestMakeupAttendanceCommandValidator_ValidCommand_Passes()
    {
        var validator = new RegisterGuestMakeupAttendanceCommandValidator();
        var cmd = new RegisterGuestMakeupAttendanceCommand(
            SessionId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            HomeGroupId: Guid.NewGuid(),
            HomeSessionId: null,
            Reason: "تعويض حصة الأسبوع الماضي");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterGuestMakeupAttendanceCommandValidator_EmptyHomeGroup_Fails()
    {
        var validator = new RegisterGuestMakeupAttendanceCommandValidator();
        var cmd = new RegisterGuestMakeupAttendanceCommand(
            SessionId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            HomeGroupId: Guid.Empty);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterGuestMakeupAttendanceCommand.HomeGroupId));
    }

    [Fact]
    public void ScanCardAttendanceCommandValidator_ValidCommand_Passes()
    {
        var validator = new ScanCardAttendanceCommandValidator();
        var cmd = new ScanCardAttendanceCommand(
            SessionId: Guid.NewGuid(),
            RawToken: "RAW_NFC_TOKEN_VALID_9921");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ScanCardAttendanceCommandValidator_EmptyToken_Fails()
    {
        var validator = new ScanCardAttendanceCommandValidator();
        var cmd = new ScanCardAttendanceCommand(
            SessionId: Guid.NewGuid(),
            RawToken: "");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ScanCardAttendanceCommand.RawToken));
    }

    [Fact]
    public void IssueNfcCredentialCommandValidator_ValidCommand_Passes()
    {
        var validator = new IssueNfcCredentialCommandValidator();
        var cmd = new IssueNfcCredentialCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            Token: "ENC_TOKEN_ABC_778",
            CardUidDisplay: "UID-8812");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IssueNfcCredentialCommandValidator_EmptyTokenOrIds_Fails()
    {
        var validator = new IssueNfcCredentialCommandValidator();
        var cmd = new IssueNfcCredentialCommand(
            WorkspaceId: Guid.Empty,
            StudentId: Guid.Empty,
            Token: "",
            CardUidDisplay: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(IssueNfcCredentialCommand.WorkspaceId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(IssueNfcCredentialCommand.StudentId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(IssueNfcCredentialCommand.Token));
    }

    [Fact]
    public void IssueQrCredentialCommandValidator_ValidCommand_Passes()
    {
        var validator = new IssueQrCredentialCommandValidator();
        var cmd = new IssueQrCredentialCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RevokeCredentialCommandValidator_ValidCommand_Passes()
    {
        var validator = new RevokeCredentialCommandValidator();
        var cmd = new RevokeCredentialCommand(
            WorkspaceId: Guid.NewGuid(),
            CredentialId: Guid.NewGuid(),
            Reason: "فقد الطالب البطاقة وتم استبدالها");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RevokeCredentialCommandValidator_EmptyReason_Fails()
    {
        var validator = new RevokeCredentialCommandValidator();
        var cmd = new RevokeCredentialCommand(
            WorkspaceId: Guid.NewGuid(),
            CredentialId: Guid.NewGuid(),
            Reason: "");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RevokeCredentialCommand.Reason));
    }
}
