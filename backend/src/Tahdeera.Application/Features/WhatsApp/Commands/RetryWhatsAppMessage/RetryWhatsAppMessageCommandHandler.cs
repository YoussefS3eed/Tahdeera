namespace Tahdeera.Application.Features.WhatsApp.Commands.RetryWhatsAppMessage;

using MediatR;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

public sealed class RetryWhatsAppMessageCommandHandler
    : IRequestHandler<RetryWhatsAppMessageCommand, Result<RetryWhatsAppMessageResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public RetryWhatsAppMessageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RetryWhatsAppMessageResultDto>> Handle(
        RetryWhatsAppMessageCommand request,
        CancellationToken cancellationToken)
    {
        var message = await _unitOfWork.MessageOutbox.GetByIdAsync(request.MessageId, cancellationToken);
        if (message is null || message.WorkspaceId != request.WorkspaceId)
        {
            return Result.Failure<RetryWhatsAppMessageResultDto>(Error.NotFound("MESSAGE_NOT_FOUND", $"Outbox message with ID '{request.MessageId}' was not found."));
        }

        // 1. Status Gate: strictly FailedPermanent or FailedTransient
        if (message.Status == OutboxMessageStatus.Sent || message.Status == OutboxMessageStatus.Delivered)
        {
            return Result.Failure<RetryWhatsAppMessageResultDto>(DomainErrors.WhatsApp.MessageAlreadySent);
        }

        if (message.Status == OutboxMessageStatus.PublishedToBroker || message.Status == OutboxMessageStatus.Processing)
        {
            return Result.Failure<RetryWhatsAppMessageResultDto>(DomainErrors.WhatsApp.MessageInFlight);
        }

        if (message.Status != OutboxMessageStatus.FailedPermanent && message.Status != OutboxMessageStatus.FailedTransient)
        {
            return Result.Failure<RetryWhatsAppMessageResultDto>(DomainErrors.WhatsApp.InvalidStatusForRetry);
        }

        // 2. Re-query student profile to get updated guardian phone and verify opt-in
        if (!message.StudentId.HasValue)
        {
            return Result.Failure<RetryWhatsAppMessageResultDto>(DomainErrors.WhatsApp.StudentNotFound);
        }

        var student = await _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(
            request.WorkspaceId,
            message.StudentId.Value,
            cancellationToken);

        if (student is null)
        {
            return Result.Failure<RetryWhatsAppMessageResultDto>(DomainErrors.WhatsApp.StudentNotFound);
        }

        // 3. Opt-in Gate
        if (!student.WhatsappOptIn)
        {
            return Result.Failure<RetryWhatsAppMessageResultDto>(DomainErrors.WhatsApp.OptInMissing);
        }

        // 4. Refresh phone snapshot & reset state machine
        var refreshedPhone = student.GuardianPhone?.Trim();
        if (string.IsNullOrWhiteSpace(refreshedPhone))
        {
            return Result.Failure<RetryWhatsAppMessageResultDto>(Error.Validation("GUARDIAN_PHONE_REQUIRED", "Student profile has no guardian phone number."));
        }

        message.ResetForManualRetry(refreshedPhone);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var maskedPhone = MaskPhoneNumber(refreshedPhone);
        var resultDto = new RetryWhatsAppMessageResultDto(
            message.Id,
            message.WorkspaceId,
            message.Status.ToString(),
            maskedPhone,
            message.NextAttemptAt,
            "Message reset successfully and scheduled for immediate processing.");

        return Result.Success(resultDto);
    }

    private static string MaskPhoneNumber(string phone)
    {
        if (phone.Length < 7) return phone;
        return $"{phone[..3]}****{phone[^4..]}";
    }
}
