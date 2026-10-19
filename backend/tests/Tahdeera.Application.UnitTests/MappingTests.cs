using FluentAssertions;
using Mapster;
using Tahdeera.Application.Features.Credentials.DTOs;
using Tahdeera.Application.Features.ReferenceData.DTOs;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.Commands.UpdateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.ValueObjects;

namespace Tahdeera.Application.UnitTests;

public class MappingTests
{
    public MappingTests()
    {
        TestDbContextFactory.EnsureMappingsInitialized();
    }

    [Fact]
    public void Map_CreateSubscriptionPlanCommand_To_SubscriptionPlan_ShouldMapCorrectly()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Code: " teacher_pro ",
            NameAr: " باقة المعلم المحترف ",
            NameEn: " Teacher Pro ",
            DescriptionAr: " وصف ",
            DescriptionEn: " Description ",
            DefaultPrice: 1500.00m,
            Currency: "egp",
            DurationDays: 365,
            MaxStudents: 200,
            MaxStaff: 2,
            MaxBranches: 1,
            Features: new SubscriptionPlanFeaturesDto(AiAssistant: true));

        // Act
        var plan = command.Adapt<SubscriptionPlan>();

        // Assert
        plan.Code.Should().Be("TEACHER_PRO");
        plan.NameAr.Should().Be("باقة المعلم المحترف");
        plan.NameEn.Should().Be("Teacher Pro");
        plan.DescriptionAr.Should().Be("وصف");
        plan.DescriptionEn.Should().Be("Description");
        plan.DefaultPrice.Should().Be(1500.00m);
        plan.Currency.Should().Be("EGP");
        plan.DurationDays.Should().Be(365);
        plan.MaxStudents.Should().Be(200);
        plan.MaxStaff.Should().Be(2);
        plan.MaxBranches.Should().Be(1);
        plan.IsActive.Should().BeTrue();
        plan.Features.AiAssistant.Should().BeTrue();
    }

    [Fact]
    public void Map_SubscriptionPlan_To_SubscriptionPlanDto_ShouldMapCorrectly()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Id = planId,
            Code = "STARTER",
            NameAr = "أساسي",
            NameEn = "Starter",
            DescriptionAr = "وصف",
            DescriptionEn = "Desc",
            DefaultPrice = 500m,
            Currency = "EGP",
            DurationDays = 30,
            MaxStudents = 50,
            MaxStaff = 1,
            MaxBranches = 1,
            Features = new SubscriptionPlanFeatures(NfcAttendance: true, PerformanceReports: true),
            IsActive = true
        };

        // Act
        var dto = plan.Adapt<SubscriptionPlanDto>();

        // Assert
        dto.Id.Should().Be(planId);
        dto.Code.Should().Be("STARTER");
        dto.NameAr.Should().Be("أساسي");
        dto.NameEn.Should().Be("Starter");
        dto.DefaultPrice.Should().Be(500m);
        dto.Features.NfcAttendance.Should().BeTrue();
        dto.Features.PerformanceReports.Should().BeTrue();
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Adapt_UpdateSubscriptionPlanCommand_InPlace_ShouldUpdatePlan()
    {
        // Arrange
        var plan = new SubscriptionPlan
        {
            Code = "TEST",
            NameAr = "قديم",
            NameEn = "Old",
            DefaultPrice = 100m,
            Currency = "EGP",
            DurationDays = 30,
            MaxStudents = 10,
            MaxStaff = 1,
            MaxBranches = 1,
            Features = new SubscriptionPlanFeatures(),
            IsActive = true
        };

        var updateCommand = new UpdateSubscriptionPlanCommand(
            Id: plan.Id,
            NameAr: " جديد ",
            NameEn: " New ",
            DescriptionAr: null,
            DescriptionEn: null,
            DefaultPrice: 200m,
            Currency: "usd",
            DurationDays: 60,
            MaxStudents: 25,
            MaxStaff: 3,
            MaxBranches: 2,
            Features: new SubscriptionPlanFeaturesDto(WhatsappNotifications: true),
            IsActive: false);

        // Act
        updateCommand.Adapt(plan);

        // Assert
        plan.NameAr.Should().Be("جديد");
        plan.NameEn.Should().Be("New");
        plan.DefaultPrice.Should().Be(200m);
        plan.Currency.Should().Be("USD");
        plan.DurationDays.Should().Be(60);
        plan.MaxStudents.Should().Be(25);
        plan.MaxStaff.Should().Be(3);
        plan.MaxBranches.Should().Be(2);
        plan.IsActive.Should().BeFalse();
        plan.Features.WhatsappNotifications.Should().BeTrue();
    }

    [Fact]
    public void Map_Grade_To_GradeDto_ShouldMapCorrectly()
    {
        // Arrange
        var grade = new Grade
        {
            Id = 1,
            Code = "PRI_1",
            NameAr = "الصف الأول الابتدائي",
            NameEn = "Primary 1",
            SortOrder = 1,
            IsActive = true
        };

        // Act
        var dto = grade.Adapt<GradeDto>();

        // Assert
        dto.Id.Should().Be(1);
        dto.Code.Should().Be("PRI_1");
        dto.NameAr.Should().Be("الصف الأول الابتدائي");
        dto.NameEn.Should().Be("Primary 1");
        dto.Stage.Should().Be("Primary");
        dto.SortOrder.Should().Be(1);
    }

    [Fact]
    public void Map_AcademicYear_To_AcademicYearDto_ShouldMapCorrectly()
    {
        // Arrange
        var yearId = Guid.NewGuid();
        var year = new AcademicYear
        {
            Id = yearId,
            Name = "2026/2027",
            StartsOn = new DateOnly(2026, 9, 1),
            EndsOn = new DateOnly(2027, 6, 30),
            IsCurrent = true
        };

        // Act
        var dto = year.Adapt<AcademicYearDto>();

        // Assert
        dto.Id.Should().Be(yearId);
        dto.NameAr.Should().Be("2026/2027");
        dto.NameEn.Should().Be("2026/2027");
        dto.StartsOn.Should().Be(new DateOnly(2026, 9, 1));
        dto.EndsOn.Should().Be(new DateOnly(2027, 6, 30));
        dto.IsCurrent.Should().BeTrue();
    }

    [Fact]
    public void Map_StudentCredential_To_CredentialDetailDto_ShouldMapCorrectly()
    {
        // Arrange
        var credId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var issuer = new AppUser { Id = issuerId, DisplayName = "Staff User" };
        var cred = new StudentCredential
        {
            Id = credId,
            CredentialType = CredentialType.Nfc,
            CardUidDisplay = "CARD-1234",
            Label = "Main Card",
            IssuedAt = DateTimeOffset.UtcNow,
            IssuedByUser = issuer,
            IssuedByUserId = issuerId,
            RevokedAt = null
        };

        // Act
        var dto = cred.Adapt<CredentialDetailDto>();

        // Assert
        dto.Id.Should().Be(credId);
        dto.CredentialType.Should().Be("Nfc");
        dto.CardUidDisplay.Should().Be("CARD-1234");
        dto.Label.Should().Be("Main Card");
        dto.IssuedByUserName.Should().Be("Staff User");
        dto.IsActive.Should().BeTrue();
        dto.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void Map_StudentCredential_To_CredentialSummaryDto_ShouldMapCorrectly()
    {
        // Arrange
        var credId = Guid.NewGuid();
        var cred = new StudentCredential
        {
            Id = credId,
            CredentialType = CredentialType.Qr,
            CardUidDisplay = "QR-5678",
            IssuedAt = DateTimeOffset.UtcNow,
            RevokedAt = null
        };

        // Act
        var dto = cred.Adapt<CredentialSummaryDto>();

        // Assert
        dto.CredentialId.Should().Be(credId);
        dto.CredentialType.Should().Be("Qr");
        dto.CardUidDisplay.Should().Be("QR-5678");
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Map_AuditLog_To_StudentAuditLogItemDto_ShouldMapCorrectly()
    {
        // Arrange
        var user = new AppUser { DisplayName = "Admin User" };
        var log = new AuditLog
        {
            Action = "Student.Created",
            OccurredAt = DateTimeOffset.UtcNow,
            ActorUser = user
        };

        // Act
        var dto = log.Adapt<StudentAuditLogItemDto>();

        // Assert
        dto.Action.Should().Be("Student.Created");
        dto.ActorName.Should().Be("Admin User");
    }
}
