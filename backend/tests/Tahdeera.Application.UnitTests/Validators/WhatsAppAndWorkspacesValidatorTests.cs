using FluentAssertions;
using Tahdeera.Application.Features.Auth.Commands.Login;
using Tahdeera.Application.Features.Auth.Commands.RefreshToken;
using Tahdeera.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.Commands.RenewWorkspaceSubscription;
using Tahdeera.Application.Features.Users.Commands.UpdatePreferences;
using Tahdeera.Application.Features.WhatsApp.Commands.ConnectWhatsAppSender;
using Tahdeera.Application.Features.WhatsApp.Commands.RetryWhatsAppMessage;
using Tahdeera.Application.Features.WhatsApp.Commands.UpdateWorkspaceWhatsAppConfig;
using Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppMessages;
using Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppUsage;
using Tahdeera.Application.Features.Workspaces.Commands.AddWorkspaceMember;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.Commands.DeactivateWorkspaceMember;
using Tahdeera.Application.Features.Workspaces.Commands.SuspendWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;

namespace Tahdeera.Application.UnitTests.Validators;

public class WhatsAppAndWorkspacesValidatorTests
{
    [Fact]
    public void LoginCommandValidator_ValidCommand_Passes()
    {
        var validator = new LoginCommandValidator();
        var cmd = new LoginCommand("teacher@tahdeera.com", "SecretPass123!");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoginCommandValidator_EmptyCredentials_Fails()
    {
        var validator = new LoginCommandValidator();
        var cmd = new LoginCommand("", "");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "USERNAME_OR_EMAIL_REQUIRED");
        result.Errors.Should().Contain(e => e.ErrorCode == "PASSWORD_REQUIRED");
    }

    [Fact]
    public void RefreshTokenCommandValidator_ValidCommand_Passes()
    {
        var validator = new RefreshTokenCommandValidator();
        var cmd = new RefreshTokenCommand("valid_refresh_token_string");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RefreshTokenCommandValidator_EmptyToken_Fails()
    {
        var validator = new RefreshTokenCommandValidator();
        var cmd = new RefreshTokenCommand("");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "REFRESH_TOKEN_REQUIRED");
    }

    [Fact]
    public void UpdatePreferencesCommandValidator_ValidCommand_Passes()
    {
        var validator = new UpdatePreferencesCommandValidator();
        var cmd = new UpdatePreferencesCommand(Guid.NewGuid(), "ar", "dark");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdatePreferencesCommandValidator_BothEmpty_Fails()
    {
        var validator = new UpdatePreferencesCommandValidator();
        var cmd = new UpdatePreferencesCommand(Guid.NewGuid(), null, "");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "AT_LEAST_ONE_PREFERENCE_REQUIRED");
    }

    [Fact]
    public void ConnectWhatsAppSenderCommandValidator_ValidCommand_Passes()
    {
        var validator = new ConnectWhatsAppSenderCommandValidator();
        var cmd = new ConnectWhatsAppSenderCommand(
            WorkspaceId: Guid.NewGuid(),
            Code: "FB_OAUTH_CODE_xyz",
            WabaId: "waba_123456",
            PhoneNumberId: "phone_id_987",
            DisplayPhoneNumber: "+201012345678");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ConnectWhatsAppSenderCommandValidator_EmptyWabaId_Fails()
    {
        var validator = new ConnectWhatsAppSenderCommandValidator();
        var cmd = new ConnectWhatsAppSenderCommand(
            WorkspaceId: Guid.NewGuid(),
            Code: "FB_OAUTH_CODE_xyz",
            WabaId: "",
            PhoneNumberId: "phone_id_987",
            DisplayPhoneNumber: "+201012345678");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConnectWhatsAppSenderCommand.WabaId));
    }

    [Fact]
    public void RetryWhatsAppMessageCommandValidator_ValidCommand_Passes()
    {
        var validator = new RetryWhatsAppMessageCommandValidator();
        var cmd = new RetryWhatsAppMessageCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateWorkspaceWhatsAppConfigCommandValidator_ValidCommand_Passes()
    {
        var validator = new UpdateWorkspaceWhatsAppConfigCommandValidator();
        var cmd = new UpdateWorkspaceWhatsAppConfigCommand(Guid.NewGuid(), "PlatformManaged", true);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateWorkspaceWhatsAppConfigCommandValidator_InvalidMode_Fails()
    {
        var validator = new UpdateWorkspaceWhatsAppConfigCommandValidator();
        var cmd = new UpdateWorkspaceWhatsAppConfigCommand(Guid.NewGuid(), "UnsupportedMode", true);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateWorkspaceWhatsAppConfigCommand.SenderMode));
    }

    [Fact]
    public void GetWorkspaceWhatsAppMessagesQueryValidator_ValidQuery_Passes()
    {
        var validator = new GetWorkspaceWhatsAppMessagesQueryValidator();
        var query = new GetWorkspaceWhatsAppMessagesQuery(Guid.NewGuid(), true, Page: 1, PageSize: 25);

        var result = validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetWorkspaceWhatsAppMessagesQueryValidator_InvalidPagination_Fails()
    {
        var validator = new GetWorkspaceWhatsAppMessagesQueryValidator();
        var query = new GetWorkspaceWhatsAppMessagesQuery(Guid.NewGuid(), true, Page: 0, PageSize: 200);

        var result = validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetWorkspaceWhatsAppMessagesQuery.Page));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetWorkspaceWhatsAppMessagesQuery.PageSize));
    }

    [Fact]
    public void GetWorkspaceWhatsAppUsageQueryValidator_ValidQuery_Passes()
    {
        var validator = new GetWorkspaceWhatsAppUsageQueryValidator();
        var query = new GetWorkspaceWhatsAppUsageQuery(Guid.NewGuid(), "2026-10");

        var result = validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("2026-13")]
    [InlineData("26-10")]
    [InlineData("2026/10")]
    public void GetWorkspaceWhatsAppUsageQueryValidator_InvalidYearMonth_Fails(string ym)
    {
        var validator = new GetWorkspaceWhatsAppUsageQueryValidator();
        var query = new GetWorkspaceWhatsAppUsageQuery(Guid.NewGuid(), ym);

        var result = validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetWorkspaceWhatsAppUsageQuery.YearMonth));
    }

    [Fact]
    public void CreateSubscriptionPlanCommandValidator_ValidCommand_Passes()
    {
        var validator = new CreateSubscriptionPlanCommandValidator();
        var cmd = new CreateSubscriptionPlanCommand(
            Code: "PRO_MONTHLY",
            NameAr: "الباقة الاحترافية",
            NameEn: "Pro Monthly",
            DescriptionAr: "باقة سنتر",
            DescriptionEn: "Center plan",
            DefaultPrice: 1200m,
            Currency: "EGP",
            DurationDays: 30,
            MaxStudents: 500,
            MaxStaff: 5,
            MaxBranches: 2);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RenewWorkspaceSubscriptionCommandValidator_ValidCommand_Passes()
    {
        var validator = new RenewWorkspaceSubscriptionCommandValidator();
        var cmd = new RenewWorkspaceSubscriptionCommand(
            WorkspaceId: Guid.NewGuid(),
            Amount: 1500m,
            Method: "BankTransfer",
            Reference: "TXN-9988",
            Notes: "Renewal for 1 year",
            PlanId: Guid.NewGuid(),
            DurationDays: 365,
            OverrideStartsAt: null,
            OverrideEndsAt: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RenewWorkspaceSubscriptionCommandValidator_InvalidMethod_Fails()
    {
        var validator = new RenewWorkspaceSubscriptionCommandValidator();
        var cmd = new RenewWorkspaceSubscriptionCommand(
            WorkspaceId: Guid.NewGuid(),
            Amount: 1500m,
            Method: "Bitcoin",
            Reference: null,
            Notes: null,
            PlanId: null,
            DurationDays: 30,
            OverrideStartsAt: null,
            OverrideEndsAt: null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RenewWorkspaceSubscriptionCommand.Method));
    }

    [Fact]
    public void CreateWorkspaceCommandValidator_ValidCommand_Passes()
    {
        var validator = new CreateWorkspaceCommandValidator();
        var owner = new CreateOwnerDto(
            Username: "centeradmin",
            Email: "admin@center.com",
            Password: "SecurePassword123!",
            DisplayName: "أستاذ أحمد",
            Phone: "01012345678");

        var payment = new CreateInitialPaymentDto(
            Amount: 1000m,
            Method: "Cash",
            Reference: "RCP-001",
            Notes: "Initial deposit");

        var subscription = new CreateInitialSubscriptionDto(
            PlanId: Guid.NewGuid(),
            StartsAt: DateTimeOffset.UtcNow,
            EndsAt: DateTimeOffset.UtcNow.AddMonths(12),
            AgreedPrice: 1000m,
            InitialPayment: payment);

        var cmd = new CreateWorkspaceCommand(
            Name: "سنتر النخبة التعليمي",
            Kind: "Center",
            TimezoneId: "Egypt Standard Time",
            ContactPhone: "01012345678",
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 15,
            Owner: owner,
            Subscription: subscription);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateWorkspaceCommandValidator_InvalidKind_Fails()
    {
        var validator = new CreateWorkspaceCommandValidator();
        var owner = new CreateOwnerDto(
            Username: "teacher1",
            Email: "teacher@test.com",
            Password: "Password123!",
            DisplayName: "Teacher",
            Phone: null);

        var payment = new CreateInitialPaymentDto(500m, "Cash", null, null);
        var sub = new CreateInitialSubscriptionDto(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            500m,
            payment);

        var cmd = new CreateWorkspaceCommand(
            Name: "Workspace 1",
            Kind: "InvalidKind",
            TimezoneId: null,
            ContactPhone: null,
            CityAddress: null,
            DefaultLateGraceMinutes: null,
            Owner: owner,
            Subscription: sub);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateWorkspaceCommand.Kind));
    }

    [Fact]
    public void SuspendWorkspaceCommandValidator_ValidCommand_Passes()
    {
        var validator = new SuspendWorkspaceCommandValidator();
        var cmd = new SuspendWorkspaceCommand(Guid.NewGuid(), "Non-payment of annual invoice");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SuspendWorkspaceCommandValidator_EmptyReason_Fails()
    {
        var validator = new SuspendWorkspaceCommandValidator();
        var cmd = new SuspendWorkspaceCommand(Guid.NewGuid(), "");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SuspendWorkspaceCommand.Reason));
    }

    [Fact]
    public void DeactivateWorkspaceMemberCommandValidator_ValidCommand_Passes()
    {
        var validator = new DeactivateWorkspaceMemberCommandValidator();
        var cmd = new DeactivateWorkspaceMemberCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddWorkspaceMemberCommandValidator_ValidCommand_Passes()
    {
        var validator = new AddWorkspaceMemberCommandValidator();
        var cmd = new AddWorkspaceMemberCommand(
            WorkspaceId: Guid.NewGuid(),
            Username: "assistant_nour",
            Email: "nour@tahdeera.com",
            Password: "Password123!",
            DisplayName: "نور علي",
            Phone: "01123456789",
            Role: "Assistant");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
