using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Features.WhatsApp.Commands.RetryWhatsAppMessage;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;
using Xunit;

namespace Tahdeera.Application.UnitTests.WhatsApp;

public class RetryWhatsAppMessageCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RetryWhatsAppMessageCommandHandler _handler;

    public RetryWhatsAppMessageCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new RetryWhatsAppMessageCommandHandler(_unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenMessageNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var command = new RetryWhatsAppMessageCommand(workspaceId, messageId);

        _unitOfWork.MessageOutbox.GetByIdAsync(messageId, Arg.Any<CancellationToken>())
            .Returns((MessageOutbox?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MESSAGE_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WhenMessageAlreadySent_ShouldReturnMessageAlreadySent()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var command = new RetryWhatsAppMessageCommand(workspaceId, messageId);

        var message = MessageOutbox.Create(
            workspaceId, "+201012345678", "attendance_check_in_v1", "attendance_check_in_v1", "ar", "{}", "idem_1");
        message.TransitionToProcessing(TimeSpan.FromMinutes(1));
        message.TransitionToSent("wamid_123", 0.05m);

        _unitOfWork.MessageOutbox.GetByIdAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(message);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MESSAGE_ALREADY_SENT");
    }

    [Fact]
    public async Task Handle_WhenMessageInFlight_ShouldReturnMessageInFlight()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var command = new RetryWhatsAppMessageCommand(workspaceId, messageId);

        var message = MessageOutbox.Create(
            workspaceId, "+201012345678", "attendance_check_in_v1", "attendance_check_in_v1", "ar", "{}", "idem_1");
        message.TransitionToProcessing(TimeSpan.FromMinutes(1));

        _unitOfWork.MessageOutbox.GetByIdAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(message);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MESSAGE_IN_FLIGHT");
    }

    [Fact]
    public async Task Handle_WhenStudentOptedOut_ShouldReturnOptInMissing()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var command = new RetryWhatsAppMessageCommand(workspaceId, messageId);

        var message = MessageOutbox.Create(
            workspaceId, "+201012345678", "attendance_check_in_v1", "attendance_check_in_v1", "ar", "{}", "idem_1",
            studentId: studentId);
        message.TransitionToProcessing(TimeSpan.FromMinutes(1));
        message.TransitionToFailedPermanent("131026", "Recipient invalid");

        var student = new WorkspaceStudent
        {
            WorkspaceId = workspaceId,
            StudentId = studentId,
            GuardianPhone = "+201099998888",
            WhatsappOptIn = false
        };

        _unitOfWork.MessageOutbox.GetByIdAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(message);
        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(student);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OPT_IN_MISSING");
    }

    [Fact]
    public async Task Handle_WhenValidFailedPermanent_ShouldRefreshPhoneResetAttemptsAndReturnMaskedPhone()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var command = new RetryWhatsAppMessageCommand(workspaceId, messageId);

        var message = MessageOutbox.Create(
            workspaceId, "+201000000000", "attendance_check_in_v1", "attendance_check_in_v1", "ar", "{}", "idem_1",
            studentId: studentId);
        message.TransitionToProcessing(TimeSpan.FromMinutes(1));
        message.TransitionToFailedPermanent("131026", "Recipient invalid");

        var student = new WorkspaceStudent
        {
            WorkspaceId = workspaceId,
            StudentId = studentId,
            GuardianPhone = "+201012345678",
            WhatsappOptIn = true
        };

        _unitOfWork.MessageOutbox.GetByIdAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(message);
        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(student);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Pending");
        result.Value.RefreshedRecipientPhone.Should().Be("+20****5678");
        message.Attempts.Should().Be(0);
        message.RecipientPhone.Should().Be("+201012345678");

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
