using FluentAssertions;
using Tahdeera.Application.Features.Finances.Commands.BatchCreateGroupCharges;
using Tahdeera.Application.Features.Finances.Commands.CancelStudentCharge;
using Tahdeera.Application.Features.Finances.Commands.CreateStudentCharge;
using Tahdeera.Application.Features.Finances.Commands.RecordStudentPayment;
using Tahdeera.Application.Features.Finances.Commands.ReverseStudentPayment;
using Tahdeera.Application.Features.Finances.Commands.StageTuitionReminders;
using Tahdeera.Application.Features.Finances.Commands.WriteOffStudentCharge;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.Features.Finances.Queries.ExportWorkspaceFinanceCsv;
using Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceLedger;
using Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceStudents;
using Tahdeera.Application.Features.Finances.Queries.VerifyPrintableReceipt;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.UnitTests.Validators;

public class FinancesValidatorTests
{
    [Fact]
    public void BatchCreateGroupChargesValidator_ValidCommand_Passes()
    {
        var validator = new BatchCreateGroupChargesValidator();
        var cmd = new BatchCreateGroupChargesCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            BillingPeriodLabel: "2026-10",
            Description: "مصاريف شهر أكتوبر",
            Amount: 300m);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void BatchCreateGroupChargesValidator_InvalidAmountAndDescription_Fails()
    {
        var validator = new BatchCreateGroupChargesValidator();
        var cmd = new BatchCreateGroupChargesCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            BillingPeriodLabel: "2026-10",
            Description: "no",
            Amount: -10m);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BatchCreateGroupChargesCommand.Amount));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BatchCreateGroupChargesCommand.Description));
    }

    [Fact]
    public void CancelStudentChargeValidator_ValidCommand_Passes()
    {
        var validator = new CancelStudentChargeValidator();
        var cmd = new CancelStudentChargeCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            ChargeId: Guid.NewGuid(),
            Reason: "إلغاء بسبب خطأ في التسجيل المالي");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CancelStudentChargeValidator_ReasonTooShort_Fails()
    {
        var validator = new CancelStudentChargeValidator();
        var cmd = new CancelStudentChargeCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            ChargeId: Guid.NewGuid(),
            Reason: "خطأ");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "CANCELLATION_REASON_TOO_SHORT");
    }

    [Fact]
    public void CreateStudentChargeValidator_ValidCommand_Passes()
    {
        var validator = new CreateStudentChargeValidator();
        var cmd = new CreateStudentChargeCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            BillingPeriodLabel: "2026-10",
            Description: "مذكرة الشرح والمراجعة",
            Amount: 150m,
            DueAt: DateTimeOffset.UtcNow.AddDays(7),
            Notes: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateStudentChargeValidator_InvalidAmount_Fails()
    {
        var validator = new CreateStudentChargeValidator();
        var cmd = new CreateStudentChargeCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            GroupId: null,
            BillingPeriodLabel: null,
            Description: "مذكرة",
            Amount: 0m,
            DueAt: null,
            Notes: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "AMOUNT_MUST_BE_POSITIVE");
    }

    [Fact]
    public void RecordStudentPaymentValidator_ValidCommand_Passes()
    {
        var validator = new RecordStudentPaymentValidator();
        var cmd = new RecordStudentPaymentCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            Amount: 200m,
            Method: PaymentMethod.Cash,
            PaidAt: DateTimeOffset.UtcNow,
            Reference: "RCP-10023",
            Notes: "سداد نقدي من ولي الأمر",
            Allocations: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RecordStudentPaymentValidator_FutureDate_Fails()
    {
        var validator = new RecordStudentPaymentValidator();
        var cmd = new RecordStudentPaymentCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            Amount: 200m,
            Method: PaymentMethod.Cash,
            PaidAt: DateTimeOffset.UtcNow.AddDays(1),
            Reference: null,
            Notes: null,
            Allocations: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "PAYMENT_DATE_CANNOT_BE_FUTURE");
    }

    [Fact]
    public void RecordStudentPaymentValidator_AllocationsExceedAmount_Fails()
    {
        var validator = new RecordStudentPaymentValidator();
        var allocations = new List<PaymentAllocationRequestDto>
        {
            new(Guid.NewGuid(), 150m),
            new(Guid.NewGuid(), 100m)
        };
        var cmd = new RecordStudentPaymentCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            Amount: 200m,
            Method: PaymentMethod.Cash,
            PaidAt: DateTimeOffset.UtcNow,
            Reference: null,
            Notes: null,
            Allocations: allocations);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "ALLOCATION_EXCEEDS_PAYMENT");
    }

    [Fact]
    public void ReverseStudentPaymentValidator_ValidCommand_Passes()
    {
        var validator = new ReverseStudentPaymentValidator();
        var cmd = new ReverseStudentPaymentCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            PaymentId: Guid.NewGuid(),
            Reason: "إلغاء عملية الدفع بناء على طلب ولي الأمر");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void StageTuitionRemindersValidator_ValidCommand_Passes()
    {
        var validator = new StageTuitionRemindersValidator();
        var cmd = new StageTuitionRemindersCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: null,
            MinimumBalanceThreshold: 50m);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void WriteOffStudentChargeValidator_ValidCommand_Passes()
    {
        var validator = new WriteOffStudentChargeValidator();
        var cmd = new WriteOffStudentChargeCommand(
            WorkspaceId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            ChargeId: Guid.NewGuid(),
            Reason: "إعفاء كامل لظروف عائلية استثنائية");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ExportWorkspaceFinanceCsvValidator_DateSpanExceeded_Fails()
    {
        var validator = new ExportWorkspaceFinanceCsvValidator();
        var from = DateTimeOffset.UtcNow.AddDays(-100);
        var to = DateTimeOffset.UtcNow;
        var query = new ExportWorkspaceFinanceCsvQuery(
            WorkspaceId: Guid.NewGuid(),
            FromDate: from,
            ToDate: to);

        var result = validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "EXPORT_DATE_SPAN_EXCEEDED");
    }

    [Fact]
    public void GetWorkspaceFinanceLedgerValidator_PageSizeExceeded_Fails()
    {
        var validator = new GetWorkspaceFinanceLedgerValidator();
        var query = new GetWorkspaceFinanceLedgerQuery(
            WorkspaceId: Guid.NewGuid(),
            PageIndex: 0,
            PageSize: 150);

        var result = validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetWorkspaceFinanceLedgerQuery.PageSize));
    }

    [Fact]
    public void GetWorkspaceFinanceStudentsValidator_InvalidStatus_Fails()
    {
        var validator = new GetWorkspaceFinanceStudentsValidator();
        var query = new GetWorkspaceFinanceStudentsQuery(
            WorkspaceId: Guid.NewGuid(),
            PageIndex: 0,
            PageSize: 20,
            Status: "illegal_status");

        var result = validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "INVALID_FINANCE_STATUS_FILTER");
    }

    [Fact]
    public void VerifyPrintableReceiptValidator_ValidHash_Passes()
    {
        var validator = new VerifyPrintableReceiptValidator();
        var validSha256 = new string('a', 64);
        var query = new VerifyPrintableReceiptQuery("RCP-2026-0001", validSha256);

        var result = validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPrintableReceiptValidator_InvalidHash_Fails()
    {
        var validator = new VerifyPrintableReceiptValidator();
        var query = new VerifyPrintableReceiptQuery("RCP-2026-0001", "not-a-valid-sha256");

        var result = validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "INVALID_RECEIPT_VERIFICATION_TOKEN");
    }
}
