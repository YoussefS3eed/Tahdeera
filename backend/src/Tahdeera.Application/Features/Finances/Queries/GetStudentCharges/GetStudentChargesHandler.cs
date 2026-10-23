using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Application.Features.Finances.Queries.GetStudentCharges;

public class GetStudentChargesHandler : IRequestHandler<GetStudentChargesQuery, List<StudentChargeDto>>
{
    private readonly ITahdeeraDbContext _context;

    public GetStudentChargesHandler(ITahdeeraDbContext context)
    {
        _context = context;
    }

    public async Task<List<StudentChargeDto>> Handle(GetStudentChargesQuery request, CancellationToken cancellationToken)
    {
        var ws = await _context.WorkspaceStudents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                w => w.WorkspaceId == request.WorkspaceId && w.StudentId == request.StudentId,
                cancellationToken);

        if (ws == null)
        {
            throw new DomainRuleViolationException(
                "STUDENT_NOT_IN_WORKSPACE",
                "The student is not enrolled in this workspace.",
                HttpStatusCode.NotFound);
        }

        var account = await _context.StudentAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.WorkspaceStudentId == ws.Id, cancellationToken);

        if (account == null)
        {
            return new List<StudentChargeDto>();
        }

        var chargesQuery = _context.StudentCharges
            .AsNoTracking()
            .Include(c => c.Group)
            .Include(c => c.Allocations)
            .Where(c => c.StudentAccountId == account.Id);

        if (request.Status.HasValue)
        {
            chargesQuery = chargesQuery.Where(c => c.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.BillingPeriod))
        {
            var period = request.BillingPeriod.Trim();
            chargesQuery = chargesQuery.Where(c => c.BillingPeriodLabel == period);
        }

        var charges = await chargesQuery
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return charges.Select(c =>
        {
            var settled = c.Allocations.Where(a => !a.IsReversed).Sum(a => a.Amount);
            var remaining = Math.Max(0.00m, c.Amount - settled);

            return new StudentChargeDto(
                c.Id,
                c.StudentAccountId,
                c.GroupId,
                c.Group?.Name,
                c.BillingPeriodLabel,
                c.Description,
                c.Amount,
                settled,
                remaining,
                c.DueAt,
                c.Status.ToString(),
                c.Notes,
                c.CreatedAt);
        }).ToList();
    }
}
