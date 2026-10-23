using FluentAssertions;
using System.Net;
using Tahdeera.Domain.Common;

namespace Tahdeera.Domain.UnitTests.Common;

public class DomainCommonTests
{
    private class TestEntity : BaseEntity
    {
    }

    [Fact]
    public void BaseEntity_SetsDefaultIdAndUtcTimestamps()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().NotBeEmpty();
        entity.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        entity.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void DomainException_Constructors_SetPropertiesCorrectly()
    {
        // Act 1: Standard constructor defaults to BadRequest
        var ex1 = new DomainException("SOME_ERROR", "An error occurred");
        ex1.ErrorCode.Should().Be("SOME_ERROR");
        ex1.Message.Should().Be("An error occurred");
        ex1.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Act 2: Custom status code
        var ex2 = new DomainException("NOT_FOUND", "Resource missing", HttpStatusCode.NotFound);
        ex2.ErrorCode.Should().Be("NOT_FOUND");
        ex2.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Act 3: With inner exception
        var inner = new InvalidOperationException("Inner failure");
        var ex3 = new DomainException("CONFLICT", "Conflict detected", inner, HttpStatusCode.Conflict);
        ex3.ErrorCode.Should().Be("CONFLICT");
        ex3.InnerException.Should().BeSameAs(inner);
        ex3.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public void Error_StaticAndFactoryMethods_CreateCorrectTypes()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Description.Should().BeEmpty();
        Error.None.Type.Should().Be(ErrorType.Failure);

        Error.NullValue.Code.Should().Be("Error.NullValue");
        Error.NullValue.Type.Should().Be(ErrorType.Failure);

        var fail = Error.Failure("FAIL_01", "Failure message");
        fail.Type.Should().Be(ErrorType.Failure);
        fail.Code.Should().Be("FAIL_01");

        var val = Error.Validation("VAL_01", "Validation error");
        val.Type.Should().Be(ErrorType.Validation);
        val.Code.Should().Be("VAL_01");

        var nf = Error.NotFound("NF_01", "Not found error");
        nf.Type.Should().Be(ErrorType.NotFound);

        var conf = Error.Conflict("CONF_01", "Conflict error");
        conf.Type.Should().Be(ErrorType.Conflict);

        var forb = Error.Forbidden("FORB_01", "Forbidden error");
        forb.Type.Should().Be(ErrorType.Forbidden);

        var unauth = Error.Unauthorized("UNAUTH_01", "Unauthorized error");
        unauth.Type.Should().Be(ErrorType.Unauthorized);

        var unproc = Error.Unprocessable("UNPROC_01", "Unprocessable error");
        unproc.Type.Should().Be(ErrorType.Unprocessable);

        // Implicit string conversion
        string errorCodeStr = val;
        errorCodeStr.Should().Be("VAL_01");
    }

    [Fact]
    public void DomainErrors_Groups_DefineValidErrors()
    {
        DomainErrors.Groups.NameDuplicate.Code.Should().Be("GROUP_NAME_DUPLICATE");
        DomainErrors.Groups.NotFound.Code.Should().Be("GROUP_NOT_FOUND");
        DomainErrors.Groups.InvalidGradeId.Code.Should().Be("INVALID_GRADE_ID");
        DomainErrors.Groups.InvalidAbsenceThreshold.Code.Should().Be("INVALID_ABSENCE_THRESHOLD");
        DomainErrors.Groups.GradeMismatch.Code.Should().Be("GROUP_GRADE_MISMATCH");
        DomainErrors.Groups.StudentWorkspaceStatusNotActive.Code.Should().Be("STUDENT_WORKSPACE_STATUS_NOT_ACTIVE");
        DomainErrors.Groups.StudentAlreadyEnrolled.Code.Should().Be("STUDENT_ALREADY_ENROLLED_IN_GROUP");
        DomainErrors.Groups.StudentSuspendedRequiresReinstatement.Code.Should().Be("STUDENT_ALREADY_ENROLLED_IN_GROUP");
        DomainErrors.Groups.StudentNotEnrolled.Code.Should().Be("STUDENT_NOT_ENROLLED_IN_GROUP");
        DomainErrors.Groups.CapacityExceeded.Code.Should().Be("GROUP_CAPACITY_EXCEEDED");
        DomainErrors.Groups.ArchivedCannotSchedule.Code.Should().Be("GROUP_ARCHIVED_CANNOT_SCHEDULE");
        DomainErrors.Groups.DelegationNotFound.Code.Should().Be("DELEGATION_NOT_FOUND");
        DomainErrors.Groups.AssistantNotAuthorizedDelegate.Code.Should().Be("ASSISTANT_NOT_AUTHORIZED_DELEGATE");
        DomainErrors.Groups.CorrectionReasonRequired.Code.Should().Be("CORRECTION_REASON_REQUIRED");
        DomainErrors.Groups.ConcurrentDelegationConflict.Code.Should().Be("CONCURRENT_DELEGATION_CONFLICT");
    }

    [Fact]
    public void DomainErrors_Sessions_DefineValidErrors()
    {
        DomainErrors.Sessions.NotFound.Code.Should().Be("SESSION_NOT_FOUND");
        DomainErrors.Sessions.InvalidTimeRange.Code.Should().Be("SESSION_INVALID_TIME_RANGE");
        DomainErrors.Sessions.CannotCompleteBeforeStart.Code.Should().Be("SESSION_CANNOT_COMPLETE_BEFORE_START");
        DomainErrors.Sessions.AlreadyCompletedScheduleFrozen.Code.Should().Be("SESSION_ALREADY_COMPLETED_SCHEDULE_FROZEN");
        DomainErrors.Sessions.CancellationReasonRequired.Code.Should().Be("CANCELLATION_REASON_REQUIRED");
        DomainErrors.Sessions.UnauthorizedWorkspaceAccess.Code.Should().Be("UNAUTHORIZED_WORKSPACE_ACCESS");
    }

    [Fact]
    public void DomainErrors_Attendance_DefineValidErrors()
    {
        DomainErrors.Attendance.CredentialNotFound.Code.Should().Be("CREDENTIAL_NOT_FOUND");
        DomainErrors.Attendance.CredentialRevoked.Code.Should().Be("CREDENTIAL_REVOKED");
        DomainErrors.Attendance.StudentBlockedInWorkspace.Code.Should().Be("STUDENT_BLOCKED_IN_WORKSPACE");
        DomainErrors.Attendance.StudentNotEnrolledInHomeGroup.Code.Should().Be("STUDENT_NOT_ENROLLED_IN_HOME_GROUP");
        DomainErrors.Attendance.HomeSessionNotEligibleForExcusal.Code.Should().Be("HOME_SESSION_NOT_ELIGIBLE_FOR_EXCUSAL");
        DomainErrors.Attendance.GradeMismatch.Code.Should().Be("GROUP_GRADE_MISMATCH");
        DomainErrors.Attendance.CorrectionReasonRequired.Code.Should().Be("CORRECTION_REASON_REQUIRED");
        DomainErrors.Attendance.RetroactiveReasonRequired.Code.Should().Be("RETROACTIVE_REASON_REQUIRED");
        DomainErrors.Attendance.CorrectionWindowExpired.Code.Should().Be("ATTENDANCE_CORRECTION_WINDOW_EXPIRED");
        DomainErrors.Attendance.NotFound.Code.Should().Be("ATTENDANCE_RECORD_NOT_FOUND");
        DomainErrors.Attendance.AlreadyVoided.Code.Should().Be("ATTENDANCE_RECORD_ALREADY_VOIDED");
        DomainErrors.Attendance.InvalidAutoAbsenceBuffer.Code.Should().Be("INVALID_AUTO_ABSENCE_BUFFER");
        DomainErrors.Attendance.SessionNotActive.Code.Should().Be("SESSION_NOT_ACTIVE");
    }

    [Fact]
    public void DomainErrors_WhatsApp_DefineValidErrors()
    {
        DomainErrors.WhatsApp.WebhookModeInvalid.Code.Should().Be("WEBHOOK_MODE_INVALID");
        DomainErrors.WhatsApp.WebhookVerifyTokenMismatch.Code.Should().Be("WEBHOOK_VERIFY_TOKEN_MISMATCH");
        DomainErrors.WhatsApp.WebhookSignatureInvalid.Code.Should().Be("WEBHOOK_SIGNATURE_INVALID");
        DomainErrors.WhatsApp.QuotaExceeded.Code.Should().Be("WORKSPACE_QUOTA_EXCEEDED");
        DomainErrors.WhatsApp.SenderNotConfigured.Code.Should().Be("SENDER_NOT_CONFIGURED");
        DomainErrors.WhatsApp.InvalidTemplateVariable.Code.Should().Be("INVALID_TEMPLATE_VARIABLE");
        DomainErrors.WhatsApp.OptInMissing.Code.Should().Be("OPT_IN_MISSING");
        DomainErrors.WhatsApp.MessageAlreadySent.Code.Should().Be("MESSAGE_ALREADY_SENT");
        DomainErrors.WhatsApp.MessageInFlight.Code.Should().Be("MESSAGE_IN_FLIGHT");
        DomainErrors.WhatsApp.InvalidStatusForRetry.Code.Should().Be("INVALID_STATUS_FOR_RETRY");
        DomainErrors.WhatsApp.StudentNotFound.Code.Should().Be("STUDENT_NOT_FOUND");
        DomainErrors.WhatsApp.RetryUnauthorizedRole.Code.Should().Be("RETRY_UNAUTHORIZED_ROLE");
        DomainErrors.WhatsApp.MetaServiceUnavailable.Code.Should().Be("META_SERVICE_UNAVAILABLE");
        DomainErrors.WhatsApp.WabaConnectionNotFound.Code.Should().Be("WABA_CONNECTION_NOT_FOUND");
        DomainErrors.WhatsApp.TemplateNotFound.Code.Should().Be("TEMPLATE_NOT_FOUND");
    }
}
