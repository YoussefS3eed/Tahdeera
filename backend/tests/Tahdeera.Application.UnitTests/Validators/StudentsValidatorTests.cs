using FluentAssertions;
using Tahdeera.Application.Features.Students.Commands.ChangeStudentStatus;
using Tahdeera.Application.Features.Students.Commands.LinkStudentByCard;
using Tahdeera.Application.Features.Students.Commands.LinkStudentByCode;
using Tahdeera.Application.Features.Students.Commands.RegisterStudent;
using Tahdeera.Application.Features.Students.Commands.UpdateStudentContact;

namespace Tahdeera.Application.UnitTests.Validators;

public class StudentsValidatorTests
{
    [Fact]
    public void RegisterStudentCommandValidator_ValidCommand_Passes()
    {
        var validator = new RegisterStudentCommandValidator();
        var cmd = new RegisterStudentCommand(
            WorkspaceId: Guid.NewGuid(),
            FullName: "محمد أحمد علي",
            GradeId: 3,
            GuardianName: "أحمد علي",
            GuardianPhone: "01012345678",
            StudentPhone: "01123456789",
            WhatsappOptIn: true,
            SeatNumber: "A-12",
            LocalNotes: "طالب متفوق",
            CardToken: "token_abc123",
            CardUidDisplay: "CARD-991");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("   ")]
    public void RegisterStudentCommandValidator_InvalidFullName_Fails(string name)
    {
        var validator = new RegisterStudentCommandValidator();
        var cmd = new RegisterStudentCommand(
            WorkspaceId: Guid.NewGuid(),
            FullName: name,
            GradeId: 3,
            GuardianName: "ولي الأمر",
            GuardianPhone: "01012345678",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            CardToken: null,
            CardUidDisplay: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStudentCommand.FullName));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void RegisterStudentCommandValidator_InvalidGradeId_Fails(short gradeId)
    {
        var validator = new RegisterStudentCommandValidator();
        var cmd = new RegisterStudentCommand(
            WorkspaceId: Guid.NewGuid(),
            FullName: "محمد علي",
            GradeId: gradeId,
            GuardianName: "ولي الأمر",
            GuardianPhone: "01012345678",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            CardToken: null,
            CardUidDisplay: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStudentCommand.GradeId));
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("01312345678")]
    [InlineData("0101234567")]
    [InlineData("invalid_phone")]
    public void RegisterStudentCommandValidator_InvalidPhone_Fails(string phone)
    {
        var validator = new RegisterStudentCommandValidator();
        var cmd = new RegisterStudentCommand(
            WorkspaceId: Guid.NewGuid(),
            FullName: "محمد علي",
            GradeId: 1,
            GuardianName: "ولي الأمر",
            GuardianPhone: phone,
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            CardToken: null,
            CardUidDisplay: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStudentCommand.GuardianPhone));
    }

    [Fact]
    public void LinkStudentByCodeCommandValidator_ValidCommand_Passes()
    {
        var validator = new LinkStudentByCodeCommandValidator();
        var cmd = new LinkStudentByCodeCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentCode: "STU-2026-12345",
            VerificationFactor: "01012345678",
            GradeId: 2,
            GuardianName: "ولي الأمر",
            GuardianPhone: "01012345678",
            StudentPhone: null,
            WhatsappOptIn: true,
            SeatNumber: "B1",
            LocalNotes: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("STU-123")]
    [InlineData("STU-2026-123")]
    [InlineData("stu-2026-12345")]
    [InlineData("STU-202-12345")]
    public void LinkStudentByCodeCommandValidator_InvalidCodeFormat_Fails(string code)
    {
        var validator = new LinkStudentByCodeCommandValidator();
        var cmd = new LinkStudentByCodeCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentCode: code,
            VerificationFactor: "01012345678",
            GradeId: 2,
            GuardianName: null,
            GuardianPhone: "01012345678",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LinkStudentByCodeCommand.StudentCode));
    }

    [Fact]
    public void LinkStudentByCardCommandValidator_ValidCommand_Passes()
    {
        var validator = new LinkStudentByCardCommandValidator();
        var cmd = new LinkStudentByCardCommand(
            WorkspaceId: Guid.NewGuid(),
            ScannedToken: "RAW_NFC_TOKEN_123456",
            GradeId: 3,
            GuardianName: "ولي الأمر",
            GuardianPhone: "01212345678",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LinkStudentByCardCommandValidator_EmptyToken_Fails()
    {
        var validator = new LinkStudentByCardCommandValidator();
        var cmd = new LinkStudentByCardCommand(
            WorkspaceId: Guid.NewGuid(),
            ScannedToken: "",
            GradeId: 3,
            GuardianName: null,
            GuardianPhone: "01212345678",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LinkStudentByCardCommand.ScannedToken));
    }

    [Fact]
    public void UpdateStudentContactCommandValidator_ValidCommand_Passes()
    {
        var validator = new UpdateStudentContactCommandValidator();
        var cmd = new UpdateStudentContactCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            FullName: "محمود حسن",
            GuardianName: "حسن",
            GuardianPhone: "01512345678",
            StudentPhone: "+201098765432",
            WhatsappOptIn: true,
            SeatNumber: "B-5",
            LocalNotes: "Updated notes");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateStudentContactCommandValidator_EmptyWorkspaceOrStudentId_Fails()
    {
        var validator = new UpdateStudentContactCommandValidator();
        var cmd = new UpdateStudentContactCommand(
            WorkspaceId: Guid.Empty,
            StudentId: Guid.Empty,
            FullName: null,
            GuardianName: null,
            GuardianPhone: null,
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStudentContactCommand.WorkspaceId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStudentContactCommand.StudentId));
    }

    [Fact]
    public void ChangeStudentStatusCommandValidator_ValidCommand_Passes()
    {
        var validator = new ChangeStudentStatusCommandValidator();
        var cmd = new ChangeStudentStatusCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            Status: "Active",
            Reason: "Reactivated by admin");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangeStudentStatusCommandValidator_InvalidStatus_Fails()
    {
        var validator = new ChangeStudentStatusCommandValidator();
        var cmd = new ChangeStudentStatusCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            Status: "NonExistentStatus",
            Reason: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangeStudentStatusCommand.Status));
    }
}
