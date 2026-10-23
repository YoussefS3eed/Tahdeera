using MediatR;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceStudents;

public class GetWorkspaceFinanceStudentsHandler : IRequestHandler<GetWorkspaceFinanceStudentsQuery, PagedResult<WorkspaceFinanceStudentItemDto>>
{
    private readonly ITahdeeraDbContext _context;

    public GetWorkspaceFinanceStudentsHandler(ITahdeeraDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<WorkspaceFinanceStudentItemDto>> Handle(
        GetWorkspaceFinanceStudentsQuery request,
        CancellationToken cancellationToken)
    {
        var baseQuery = _context.WorkspaceStudents
            .AsNoTracking()
            .Include(ws => ws.Student)
                .ThenInclude(s => s.AcademicHistories)
                    .ThenInclude(ah => ah.Grade)
            .Include(ws => ws.Student)
                .ThenInclude(s => s.GroupEnrollments)
                    .ThenInclude(ge => ge.Group)
                        .ThenInclude(g => g.Grade)
            .Include(ws => ws.StudentAccount)
                .ThenInclude(sa => sa!.Charges)
            .Include(ws => ws.StudentAccount)
                .ThenInclude(sa => sa!.Payments)
            .Where(ws => ws.WorkspaceId == request.WorkspaceId);

        if (request.GroupId.HasValue)
        {
            var targetGroupId = request.GroupId.Value;
            baseQuery = baseQuery.Where(ws =>
                _context.GroupStudents.Any(gs => gs.GroupId == targetGroupId && gs.StudentId == ws.StudentId && gs.Status == GroupStudentStatus.Active));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(ws =>
                ws.Student.FullName.ToLower().Contains(term) ||
                ws.Student.StudentCode.ToLower().Contains(term) ||
                (ws.GuardianPhone != null && ws.GuardianPhone.Contains(term)));
        }

        var students = await baseQuery.ToListAsync(cancellationToken);

        var list = new List<WorkspaceFinanceStudentItemDto>(students.Count);

        foreach (var ws in students)
        {
            var student = ws.Student;
            var account = ws.StudentAccount;

            decimal requiredFee = 0.00m;
            decimal paidAmount = 0.00m;
            DateTimeOffset? lastPaymentDate = null;

            if (account != null)
            {
                requiredFee = account.Charges
                    .Where(c => c.Status == ChargeStatus.Open)
                    .Sum(c => c.Amount);

                var receivedPayments = account.Payments
                    .Where(p => p.Status == PaymentStatus.Received)
                    .ToList();

                paidAmount = receivedPayments.Sum(p => p.Amount);

                lastPaymentDate = receivedPayments
                    .OrderByDescending(p => p.PaidAt)
                    .Select(p => (DateTimeOffset?)p.PaidAt)
                    .FirstOrDefault();
            }

            decimal remainingFee = Math.Max(0.00m, requiredFee - paidAmount);

            string paymentStatus;
            if (requiredFee == 0.00m)
            {
                paymentStatus = "Settled";
            }
            else if (remainingFee <= 0.00m)
            {
                paymentStatus = "Paid";
            }
            else if (paidAmount > 0.00m)
            {
                paymentStatus = "Partial";
            }
            else
            {
                paymentStatus = "Unpaid";
            }

            // Group & Grade resolution
            var activeEnrollments = student.GroupEnrollments
                .Where(ge => ge.Group != null &&
                             ge.Group.OwnerWorkspaceId == request.WorkspaceId &&
                             ge.Status == GroupStudentStatus.Active)
                .ToList();

            var preferredEnrollment = request.GroupId.HasValue
                ? activeEnrollments.FirstOrDefault(ge => ge.GroupId == request.GroupId.Value) ?? activeEnrollments.FirstOrDefault()
                : activeEnrollments.FirstOrDefault();

            string? groupName = preferredEnrollment?.Group?.Name;
            string? gradeName = preferredEnrollment?.Group?.Grade?.NameAr;

            if (string.IsNullOrWhiteSpace(gradeName))
            {
                var academicGrade = student.AcademicHistories
                    .OrderByDescending(ah => ah.RecordedAt)
                    .Select(ah => ah.Grade?.NameAr ?? ah.Grade?.NameEn)
                    .FirstOrDefault();

                gradeName = academicGrade;
            }

            list.Add(new WorkspaceFinanceStudentItemDto(
                StudentId: student.Id,
                StudentName: student.FullName,
                StudentCode: student.StudentCode,
                GradeName: gradeName,
                GroupName: groupName,
                RequiredFee: requiredFee,
                PaidAmount: paidAmount,
                RemainingFee: remainingFee,
                PaymentStatus: paymentStatus,
                LastPaymentDate: lastPaymentDate));
        }

        // Apply Status Filter if specified and not "all"
        IEnumerable<WorkspaceFinanceStudentItemDto> filtered = list;
        if (!string.IsNullOrWhiteSpace(request.Status) && !request.Status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(i => i.PaymentStatus.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = filtered
            .OrderBy(i => i.StudentName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totalCount = ordered.Count;
        var pagedItems = ordered
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResult<WorkspaceFinanceStudentItemDto>(
            Items: pagedItems,
            TotalCount: totalCount,
            PageIndex: request.PageIndex,
            PageSize: request.PageSize);
    }
}
