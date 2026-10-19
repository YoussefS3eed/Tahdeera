using Mapster;
using Tahdeera.Application.Features.Auth.DTOs;
using Tahdeera.Application.Features.Credentials.DTOs;
using Tahdeera.Application.Features.ReferenceData.DTOs;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.Commands.UpdateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.ValueObjects;

namespace Tahdeera.Application.Common.Mappings;

public class MappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // 1. SubscriptionPlanFeatures <-> SubscriptionPlanFeaturesDto
        config.NewConfig<SubscriptionPlanFeatures, SubscriptionPlanFeaturesDto>();
        config.NewConfig<SubscriptionPlanFeaturesDto, SubscriptionPlanFeatures>();

        // 2. SubscriptionPlan -> SubscriptionPlanDto
        config.NewConfig<SubscriptionPlan, SubscriptionPlanDto>()
            .Map(dest => dest.Features, src => src.Features.Adapt<SubscriptionPlanFeaturesDto>());

        // 3. CreateSubscriptionPlanCommand -> SubscriptionPlan
        config.NewConfig<CreateSubscriptionPlanCommand, SubscriptionPlan>()
            .Map(dest => dest.Code, src => src.Code.Trim().ToUpperInvariant())
            .Map(dest => dest.NameAr, src => src.NameAr.Trim())
            .Map(dest => dest.NameEn, src => src.NameEn.Trim())
            .Map(dest => dest.DescriptionAr, src => src.DescriptionAr != null ? src.DescriptionAr.Trim() : null)
            .Map(dest => dest.DescriptionEn, src => src.DescriptionEn != null ? src.DescriptionEn.Trim() : null)
            .Map(dest => dest.Currency, src => string.IsNullOrWhiteSpace(src.Currency) ? "EGP" : src.Currency.Trim().ToUpperInvariant())
            .Map(dest => dest.Features, src => (src.Features ?? new SubscriptionPlanFeaturesDto()).Adapt<SubscriptionPlanFeatures>())
            .Map(dest => dest.IsActive, _ => true);

        // 4. UpdateSubscriptionPlanCommand -> SubscriptionPlan
        config.NewConfig<UpdateSubscriptionPlanCommand, SubscriptionPlan>()
            .Map(dest => dest.NameAr, src => src.NameAr.Trim())
            .Map(dest => dest.NameEn, src => src.NameEn.Trim())
            .Map(dest => dest.DescriptionAr, src => src.DescriptionAr != null ? src.DescriptionAr.Trim() : null)
            .Map(dest => dest.DescriptionEn, src => src.DescriptionEn != null ? src.DescriptionEn.Trim() : null)
            .Map(dest => dest.Currency, src => string.IsNullOrWhiteSpace(src.Currency) ? "EGP" : src.Currency.Trim().ToUpperInvariant())
            .Map(dest => dest.Features, src => src.Features.Adapt<SubscriptionPlanFeatures>())
            .Map(dest => dest.IsActive, src => src.IsActive);

        // 5. Grade -> GradeDto
        config.NewConfig<Grade, GradeDto>()
            .Map(dest => dest.Stage, src => src.SortOrder <= 6 ? "Primary" : src.SortOrder <= 9 ? "Preparatory" : "Secondary");

        // 6. AcademicYear -> AcademicYearDto
        config.NewConfig<AcademicYear, AcademicYearDto>()
            .Map(dest => dest.NameAr, src => src.Name)
            .Map(dest => dest.NameEn, src => src.Name);

        // 7. StudentCredential -> CredentialDetailDto
        config.NewConfig<StudentCredential, CredentialDetailDto>()
            .Map(dest => dest.CredentialType, src => src.CredentialType.ToString())
            .Map(dest => dest.IssuedByUserName, src => src.IssuedByUser != null ? src.IssuedByUser.DisplayName : null)
            .Map(dest => dest.RevokedByUserName, src => src.RevokedByUser != null ? src.RevokedByUser.DisplayName : null)
            .Map(dest => dest.IsActive, src => src.RevokedAt == null);

        // 8. StudentCredential -> CredentialSummaryDto
        config.NewConfig<StudentCredential, CredentialSummaryDto>()
            .Map(dest => dest.CredentialId, src => src.Id)
            .Map(dest => dest.CredentialType, src => src.CredentialType.ToString())
            .Map(dest => dest.IsActive, src => src.RevokedAt == null);

        // 9. AuditLog -> StudentAuditLogItemDto
        config.NewConfig<AuditLog, StudentAuditLogItemDto>()
            .Map(dest => dest.ActorName, src => src.ActorUser != null ? src.ActorUser.DisplayName : "System");

        // 10. StudentCredential -> CredentialIssuedDto
        config.NewConfig<StudentCredential, CredentialIssuedDto>()
            .Map(dest => dest.CredentialId, src => src.Id)
            .Map(dest => dest.CredentialType, src => src.CredentialType.ToString());

        // 11. StudentCredential -> QrCredentialIssuedDto
        config.NewConfig<StudentCredential, QrCredentialIssuedDto>()
            .Map(dest => dest.CredentialId, src => src.Id);

        // 12. WorkspaceStudent -> StudentEnrollmentDto
        config.NewConfig<WorkspaceStudent, StudentEnrollmentDto>()
            .Map(dest => dest.StudentId, src => src.StudentId)
            .Map(dest => dest.StudentCode, src => src.Student != null ? src.Student.StudentCode : string.Empty)
            .Map(dest => dest.FullName, src => src.Student != null ? src.Student.FullName : string.Empty)
            .Map(dest => dest.Status, src => src.Status.ToString());

        // 13. Workspace -> CreateWorkspaceResultDto
        config.NewConfig<Workspace, CreateWorkspaceResultDto>()
            .Map(dest => dest.WorkspaceId, src => src.Id);

        // 14. AppUser -> UserProfileDto
        config.NewConfig<AppUser, UserProfileDto>()
            .Map(dest => dest.Email, src => src.NormalizedEmail);

        // 15. WorkspaceMember -> AddWorkspaceMemberResultDto
        config.NewConfig<WorkspaceMember, AddWorkspaceMemberResultDto>()
            .Map(dest => dest.MemberId, src => src.Id)
            .Map(dest => dest.UserId, src => src.UserId)
            .Map(dest => dest.Role, src => src.Role)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.JoinedAt, src => src.JoinedAt);

        // 16. WorkspaceMember -> DeactivateWorkspaceMemberResultDto
        config.NewConfig<WorkspaceMember, DeactivateWorkspaceMemberResultDto>()
            .Map(dest => dest.MemberId, src => src.Id)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.LeftAt, src => src.LeftAt != null ? src.LeftAt.Value : default);

        // 17. WorkspaceSubscription -> RenewSubscriptionResultDto
        config.NewConfig<WorkspaceSubscription, RenewSubscriptionResultDto>()
            .Map(dest => dest.SubscriptionId, src => src.Id)
            .Map(dest => dest.Status, src => src.Status)
            .Map(dest => dest.StartsAt, src => src.StartsAt)
            .Map(dest => dest.EndsAt, src => src.EndsAt)
            .Map(dest => dest.AgreedPrice, src => src.AgreedPrice);
    }
}
