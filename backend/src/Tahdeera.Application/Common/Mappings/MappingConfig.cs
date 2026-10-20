using Mapster;
using Tahdeera.Application.Features.Auth.DTOs;
using Tahdeera.Application.Features.Credentials.DTOs;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Application.Features.ReferenceData.DTOs;
using Tahdeera.Application.Features.Sessions.DTOs;
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

        // 18. Group -> GroupDto
        config.NewConfig<Group, GroupDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.WorkspaceId, src => src.OwnerWorkspaceId)
            .Map(dest => dest.AcademicYearId, src => src.AcademicYearId)
            .Map(dest => dest.GradeId, src => src.GradeId)
            .Map(dest => dest.SubjectId, src => src.SubjectId)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.RoomName, src => src.RoomName)
            .Map(dest => dest.ScheduleSummary, src => src.ScheduleSummary)
            .Map(dest => dest.LateGraceMinutes, src => src.LateGraceMinutes)
            .Map(dest => dest.DefaultMonthlyFee, src => src.DefaultMonthlyFee)
            .Map(dest => dest.ConsecutiveAbsenceThreshold, src => src.ConsecutiveAbsenceThreshold)
            .Map(dest => dest.AbsenceAction, src => src.AbsenceAction.ToString())
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.TargetGender, _ => "CoEd")
            .Map(dest => dest.MaxCapacity, _ => (int?)null)
            .Map(dest => dest.ActiveStudentCount, src => src.EnrolledStudents.Count(gs => gs.Status == Domain.Enums.GroupStudentStatus.Active))
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        // 19. Group -> GroupDetailDto
        config.NewConfig<Group, GroupDetailDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.WorkspaceId, src => src.OwnerWorkspaceId)
            .Map(dest => dest.AcademicYearId, src => src.AcademicYearId)
            .Map(dest => dest.AcademicYearName, src => src.AcademicYear != null ? src.AcademicYear.Name : string.Empty)
            .Map(dest => dest.GradeId, src => src.GradeId)
            .Map(dest => dest.GradeName, src => src.Grade != null ? src.Grade.NameAr : string.Empty)
            .Map(dest => dest.SubjectId, src => src.SubjectId)
            .Map(dest => dest.SubjectName, src => src.Subject != null ? src.Subject.NameAr : null)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.RoomName, src => src.RoomName)
            .Map(dest => dest.ScheduleSummary, src => src.ScheduleSummary)
            .Map(dest => dest.LateGraceMinutes, src => src.LateGraceMinutes)
            .Map(dest => dest.DefaultMonthlyFee, src => src.DefaultMonthlyFee)
            .Map(dest => dest.ConsecutiveAbsenceThreshold, src => src.ConsecutiveAbsenceThreshold)
            .Map(dest => dest.AbsenceAction, src => src.AbsenceAction.ToString())
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.TargetGender, _ => "CoEd")
            .Map(dest => dest.MaxCapacity, _ => (int?)null)
            .Map(dest => dest.ActiveStudentCount, src => src.EnrolledStudents.Count(gs => gs.Status == Domain.Enums.GroupStudentStatus.Active))
            .Map(dest => dest.TotalSessionsCount, src => src.Sessions.Count)
            .Map(dest => dest.ActiveDelegate, src => src.Delegations.Where(d => d.RevokedAt == null).Select(d => d.Adapt<GroupDelegationDto>()).FirstOrDefault())
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        // 20. GroupAttendanceDelegation -> GroupDelegationDto
        config.NewConfig<GroupAttendanceDelegation, GroupDelegationDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.GroupId, src => src.GroupId)
            .Map(dest => dest.AssistantUserId, src => src.AssistantUserId)
            .Map(dest => dest.AssistantName, src => src.AssistantUser != null ? src.AssistantUser.DisplayName : string.Empty)
            .Map(dest => dest.AssignedAt, src => src.GrantedAt)
            .Map(dest => dest.RevokedAt, src => src.RevokedAt);

        // 21. GroupWorkspaceAccess -> GroupWorkspaceAccessDto
        config.NewConfig<GroupWorkspaceAccess, GroupWorkspaceAccessDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.GroupId, src => src.GroupId)
            .Map(dest => dest.GranteeWorkspaceId, src => src.WorkspaceId)
            .Map(dest => dest.GranteeWorkspaceName, src => src.Workspace != null ? src.Workspace.Name : string.Empty)
            .Map(dest => dest.CanViewRoster, src => src.CanViewRoster)
            .Map(dest => dest.CanRegisterAttendance, src => src.CanRegisterAttendance)
            .Map(dest => dest.CanEditAttendance, src => src.CanEditAttendance)
            .Map(dest => dest.GrantedAt, src => src.GrantedAt)
            .Map(dest => dest.ExpiresAt, _ => (DateTimeOffset?)null);

        // 22. GroupStudent -> GroupStudentEnrollmentDto
        config.NewConfig<GroupStudent, GroupStudentEnrollmentDto>()
            .Map(dest => dest.GroupStudentId, src => src.Id)
            .Map(dest => dest.GroupId, src => src.GroupId)
            .Map(dest => dest.StudentId, src => src.StudentId)
            .Map(dest => dest.StudentName, src => src.Student != null ? src.Student.FullName : string.Empty)
            .Map(dest => dest.StudentCode, src => src.Student != null ? src.Student.StudentCode : string.Empty)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.JoinedAt, src => src.JoinedAt);

        // 23. Session -> SessionDto
        config.NewConfig<Session, SessionDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.GroupId, src => src.GroupId)
            .Map(dest => dest.WorkspaceId, src => src.Group != null ? src.Group.OwnerWorkspaceId : Guid.Empty)
            .Map(dest => dest.StartsAt, src => src.StartsAt)
            .Map(dest => dest.EndsAt, src => src.EndsAt)
            .Map(dest => dest.LateGraceMinutes, src => src.LateGraceMinutes)
            .Map(dest => dest.RoomName, src => src.Group != null ? src.Group.RoomName : null)
            .Map(dest => dest.Topic, _ => (string?)null)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        // 24. Session -> SessionDetailsDto
        config.NewConfig<Session, SessionDetailsDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.GroupId, src => src.GroupId)
            .Map(dest => dest.GroupName, src => src.Group != null ? src.Group.Name : string.Empty)
            .Map(dest => dest.WorkspaceId, src => src.Group != null ? src.Group.OwnerWorkspaceId : Guid.Empty)
            .Map(dest => dest.StartsAt, src => src.StartsAt)
            .Map(dest => dest.EndsAt, src => src.EndsAt)
            .Map(dest => dest.LateGraceMinutes, src => src.LateGraceMinutes)
            .Map(dest => dest.RoomName, src => src.Group != null ? src.Group.RoomName : null)
            .Map(dest => dest.Topic, _ => (string?)null)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.AutoAbsenceProcessedAt, src => src.AutoAbsenceProcessedAt)
            .Map(dest => dest.CancelledAt, src => src.CancelledAt)
            .Map(dest => dest.CancellationReason, src => src.CancellationReason)
            .Map(dest => dest.PresentCount, src => src.AttendanceRecords.Count(r => r.VoidedAt == null && r.Status == Domain.Enums.AttendanceStatus.Present))
            .Map(dest => dest.LateCount, src => src.AttendanceRecords.Count(r => r.VoidedAt == null && r.Status == Domain.Enums.AttendanceStatus.Late))
            .Map(dest => dest.AbsentCount, src => src.AttendanceRecords.Count(r => r.VoidedAt == null && r.Status == Domain.Enums.AttendanceStatus.Absent))
            .Map(dest => dest.ExcusedCount, src => src.AttendanceRecords.Count(r => r.VoidedAt == null && r.Status == Domain.Enums.AttendanceStatus.Excused))
            .Map(dest => dest.TotalRosterCount, src => src.Group != null ? src.Group.EnrolledStudents.Count(gs => gs.Status == Domain.Enums.GroupStudentStatus.Active) : 0);
    }
}
