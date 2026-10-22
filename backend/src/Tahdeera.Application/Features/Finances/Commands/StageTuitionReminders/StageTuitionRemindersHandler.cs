using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Logging;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Finances.Commands.StageTuitionReminders;

public class StageTuitionRemindersHandler : IRequestHandler<StageTuitionRemindersCommand, StageTuitionRemindersResultDto>
{
    private static readonly Regex EgyptianPhoneRegex = new(@"^(\+20|0)?1[0125][0-9]{8}$", RegexOptions.Compiled);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<StageTuitionRemindersHandler> _logger;

    public StageTuitionRemindersHandler(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ILogger<StageTuitionRemindersHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<StageTuitionRemindersResultDto> Handle(StageTuitionRemindersCommand request, CancellationToken cancellationToken)
    {
        List<Guid> targetStudentIds;

        if (request.GroupId.HasValue)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(request.GroupId.Value, cancellationToken);
            if (group == null || group.OwnerWorkspaceId != request.WorkspaceId)
            {
                throw new DomainRuleViolationException(
                    "GROUP_NOT_FOUND_IN_WORKSPACE",
                    "The specified group was not found in this workspace.",
                    HttpStatusCode.NotFound);
            }

            var groupStudents = await _unitOfWork.GroupStudents.FindAsync(
                gs => gs.GroupId == request.GroupId.Value && gs.Status == GroupStudentStatus.Active,
                cancellationToken);

            targetStudentIds = groupStudents.Select(gs => gs.StudentId).Distinct().ToList();
        }
        else
        {
            var workspaceStudents = await _unitOfWork.WorkspaceStudents.FindAsync(
                ws => ws.WorkspaceId == request.WorkspaceId && ws.Status == WorkspaceStudentStatus.Active,
                cancellationToken);

            targetStudentIds = workspaceStudents.Select(ws => ws.StudentId).Distinct().ToList();
        }

        var now = _dateTimeProvider.UtcNow;
        var cutoff = now.AddDays(-7);

        int totalDebtorsFound = 0;
        int remindersStagedCount = 0;
        int skippedCooldownCount = 0;
        int skippedOptedOutCount = 0;
        int skippedInvalidPhoneCount = 0;
        decimal totalOutstandingArrearsNotified = 0m;

        foreach (var studentId in targetStudentIds)
        {
            var ws = await _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(
                request.WorkspaceId, studentId, cancellationToken);

            if (ws == null)
            {
                continue;
            }

            var account = await _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdAsync(ws.Id, cancellationToken);
            if (account == null)
            {
                continue;
            }

            var openCharges = await _unitOfWork.StudentCharges.GetOpenChargesByAccountIdAsync(account.Id, cancellationToken);
            var payments = await _unitOfWork.StudentPayments.GetPaymentsByAccountIdWithAllocationsAsync(account.Id, cancellationToken);

            decimal totalCharges = openCharges.Sum(c => c.Amount);
            decimal totalPayments = payments.Sum(p => p.Amount);
            decimal debt = totalCharges - totalPayments;

            if (debt < request.MinimumBalanceThreshold)
            {
                continue;
            }

            totalDebtorsFound++;

            // Gate 1: Guardian WhatsApp opt-in
            if (!ws.WhatsappOptIn)
            {
                skippedOptedOutCount++;
                continue;
            }

            // Gate 2: Egyptian phone format
            var phone = ws.GuardianPhone?.Trim();
            if (string.IsNullOrWhiteSpace(phone) || !EgyptianPhoneRegex.IsMatch(phone))
            {
                skippedInvalidPhoneCount++;
                continue;
            }

            // Gate 3: 7-day anti-spam cooldown
            var hasRecentReminder = await _unitOfWork.MessageOutboxes.AnyAsync(
                m => m.WorkspaceId == request.WorkspaceId &&
                     m.StudentId == ws.StudentId &&
                     m.MessageType == "TuitionReminder" &&
                     m.CreatedAt >= cutoff,
                cancellationToken);

            if (hasRecentReminder)
            {
                skippedCooldownCount++;
                continue;
            }

            var student = await _unitOfWork.Students.GetByIdAsync(ws.StudentId, cancellationToken);
            var studentName = student?.FullName ?? "الطالب";

            var payload = JsonSerializer.Serialize(new
            {
                studentName,
                outstandingBalance = $"{debt:F2} EGP",
                dueDate = openCharges.OrderBy(c => c.DueAt ?? c.CreatedAt).FirstOrDefault()?.DueAt?.ToString("yyyy-MM-dd") ?? now.ToString("yyyy-MM-dd")
            });

            var outbox = MessageOutbox.Create(
                request.WorkspaceId,
                phone,
                "TuitionReminder",
                "tuition_reminder_v1",
                "ar",
                payload,
                $"arrears_{request.WorkspaceId}_{ws.StudentId}_{now:yyyyMMdd}",
                studentId: ws.StudentId);

            await _unitOfWork.MessageOutboxes.AddAsync(outbox, cancellationToken);
            remindersStagedCount++;
            totalOutstandingArrearsNotified += debt;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StageTuitionRemindersResultDto(
            WorkspaceId: request.WorkspaceId,
            GroupId: request.GroupId,
            MinimumBalanceThreshold: request.MinimumBalanceThreshold,
            TotalDebtorsFound: totalDebtorsFound,
            RemindersStagedCount: remindersStagedCount,
            SkippedCooldownCount: skippedCooldownCount,
            SkippedOptedOutCount: skippedOptedOutCount,
            SkippedInvalidPhoneCount: skippedInvalidPhoneCount,
            TotalOutstandingArrearsNotified: totalOutstandingArrearsNotified);
    }
}
