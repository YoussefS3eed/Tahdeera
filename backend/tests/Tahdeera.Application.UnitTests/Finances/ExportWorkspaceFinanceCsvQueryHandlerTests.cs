using System.Net;
using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Queries.ExportWorkspaceFinanceCsv;
using Tahdeera.Application.UnitTests.Helpers;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.ValueObjects;
using Xunit;

namespace Tahdeera.Application.UnitTests.Finances;

public class ExportWorkspaceFinanceCsvQueryHandlerTests
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly ExportWorkspaceFinanceCsvHandler _handler;

    public ExportWorkspaceFinanceCsvQueryHandlerTests()
    {
        _dbContext = Substitute.For<ITahdeeraDbContext>();
        _handler = new ExportWorkspaceFinanceCsvHandler(_dbContext);
    }

    [Fact]
    public async Task Handle_WithoutExcelExportFeature_ThrowsFeatureFlagDisabled()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = "Basic",
            Features = new SubscriptionPlanFeatures(ExcelExport: false)
        };

        var subscription = new WorkspaceSubscription
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Status = "Active",
            EndsAt = DateTimeOffset.UtcNow.AddMonths(1),
            Plan = plan
        };

        var subDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { subscription });
        _dbContext.WorkspaceSubscriptions.Returns(subDbSet);

        var query = new ExportWorkspaceFinanceCsvQuery(
            workspaceId,
            DateTimeOffset.UtcNow.AddDays(-10),
            DateTimeOffset.UtcNow);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("FEATURE_FLAG_DISABLED");
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Handle_WithFeatureEnabled_StreamsCsvHeaderAndRows()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();

        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = "Pro",
            Features = new SubscriptionPlanFeatures(ExcelExport: true)
        };

        var subscription = new WorkspaceSubscription
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Status = "Active",
            EndsAt = DateTimeOffset.UtcNow.AddMonths(1),
            Plan = plan
        };

        var workspace = new Workspace { Id = workspaceId, TimezoneId = "Africa/Cairo" };
        var student = new Student { Id = studentId, StudentCode = "STD-500", FullName = "Hassan Ali" };

        var ws = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            SeatNumber = "B-4",
            Student = student,
            Status = WorkspaceStudentStatus.Active
        };

        var account = StudentAccount.Create(ws.Id);
        typeof(WorkspaceStudent).GetProperty("StudentAccount")!.SetValue(ws, account);

        var payment = StudentPayment.Create(
            account.Id,
            "REC-202610-0088",
            400m,
            PaymentMethod.Cash,
            cashierId,
            paidAt: DateTimeOffset.UtcNow.AddDays(-2));

        var subDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { subscription });
        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        var workspaceDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { workspace });
        var chargeDbSet = AsyncQueryTestHelpers.CreateMockDbSet(Array.Empty<StudentCharge>());
        var paymentDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { payment });

        _dbContext.WorkspaceSubscriptions.Returns(subDbSet);
        _dbContext.Workspaces.Returns(workspaceDbSet);
        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentCharges.Returns(chargeDbSet);
        _dbContext.StudentPayments.Returns(paymentDbSet);

        var query = new ExportWorkspaceFinanceCsvQuery(
            workspaceId,
            DateTimeOffset.UtcNow.AddDays(-5),
            DateTimeOffset.UtcNow);

        // Act
        var stream = await _handler.Handle(query, CancellationToken.None);
        var lines = new List<string>();
        await foreach (var line in stream)
        {
            lines.Add(line);
        }

        // Assert
        lines.Should().HaveCount(2);
        lines[0].Should().StartWith("Date (UTC),Date (Local),Receipt/Ref");
        lines[1].Should().Contain("REC-202610-0088");
        lines[1].Should().Contain("Payment");
        lines[1].Should().Contain("STD-500");
        lines[1].Should().Contain("Hassan Ali");
        lines[1].Should().Contain("400.00");
    }

    [Fact]
    public void Validator_DateRangeExceeds90Days_HasValidationError()
    {
        // Arrange
        var validator = new ExportWorkspaceFinanceCsvValidator();
        var query = new ExportWorkspaceFinanceCsvQuery(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(-95),
            DateTimeOffset.UtcNow);

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "EXPORT_DATE_SPAN_EXCEEDED");
    }

    [Fact]
    public void Validator_FromDateAfterToDate_HasValidationError()
    {
        // Arrange
        var validator = new ExportWorkspaceFinanceCsvValidator();
        var query = new ExportWorkspaceFinanceCsvQuery(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(5),
            DateTimeOffset.UtcNow);

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "INVALID_DATE_RANGE");
    }
}
