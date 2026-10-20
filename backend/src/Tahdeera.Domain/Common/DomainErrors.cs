namespace Tahdeera.Domain.Common;

public static class DomainErrors
{
    public static class Groups
    {
        public static Error NameDuplicate =>
            Error.Conflict("GROUP_NAME_DUPLICATE", "يوجد بالفعل مجموعة بهذا الاسم في نفس العام الدراسي.");

        public static Error NotFound =>
            Error.NotFound("GROUP_NOT_FOUND", "لم يتم العثور على المجموعة المحددة.");

        public static Error InvalidGradeId =>
            Error.Unprocessable("INVALID_GRADE_ID", "المرحلة الدراسية المحددة غير صالحة (يجب أن تكون بين 1 و 12).");

        public static Error InvalidAbsenceThreshold =>
            Error.Unprocessable("INVALID_ABSENCE_THRESHOLD", "حد الغياب المتتالي يجب أن يكون بين 1 و 50 حصة.");

        public static Error GradeMismatch =>
            Error.Unprocessable("GROUP_GRADE_MISMATCH", "المرحلة الدراسية للطالب لا تتطابق مع مرحلة المجموعة.");

        public static Error StudentWorkspaceStatusNotActive =>
            Error.Validation("STUDENT_WORKSPACE_STATUS_NOT_ACTIVE", "حالة الطالب غير نشطة في مساحة العمل ولا يمكن تسجيله بالمجموعة.");

        public static Error StudentAlreadyEnrolled =>
            Error.Conflict("STUDENT_ALREADY_ENROLLED_IN_GROUP", "الطالب مسجل بالفعل في هذه المجموعة.");

        public static Error StudentSuspendedRequiresReinstatement =>
            Error.Conflict("STUDENT_ALREADY_ENROLLED_IN_GROUP", "الطالب موقوف حالياً في هذه المجموعة ويجب إعادة تفعيله إدارياً.");

        public static Error StudentNotEnrolled =>
            Error.NotFound("STUDENT_NOT_ENROLLED_IN_GROUP", "الطالب غير مسجل في هذه المجموعة.");

        public static Error CapacityExceeded =>
            Error.Unprocessable("GROUP_CAPACITY_EXCEEDED", "المجموعة وصلت للحد الأقصى من السعة الاستيعابية للطلاب.");

        public static Error ArchivedCannotSchedule =>
            Error.Validation("GROUP_ARCHIVED_CANNOT_SCHEDULE", "لا يمكن جدولة حصص لمجموعة مؤرشفة.");

        public static Error DelegationNotFound =>
            Error.NotFound("DELEGATION_NOT_FOUND", "لم يتم العثور على سجل التفويض المحدد.");

        public static Error AssistantNotAuthorizedDelegate =>
            Error.Forbidden("ASSISTANT_NOT_AUTHORIZED_DELEGATE", "المساعد غير مفوض لتسجيل الحضور لهذه المجموعة.");

        public static Error CorrectionReasonRequired =>
            Error.Validation("CORRECTION_REASON_REQUIRED", "سبب التعديل إلزامي عند تعديل حالة الطالب أو السجل.");
    }

    public static class Sessions
    {
        public static Error NotFound =>
            Error.NotFound("SESSION_NOT_FOUND", "لم يتم العثور على الحصة الدراسية المحددة.");

        public static Error InvalidTimeRange =>
            Error.Validation("SESSION_INVALID_TIME_RANGE", "وقت نهاية الحصة يجب أن يكون بعد وقت بدايتها.");

        public static Error CannotCompleteBeforeStart =>
            Error.Validation("SESSION_CANNOT_COMPLETE_BEFORE_START", "لا يمكن إنهاء الحصة الدراسية قبل موعد بدايتها المحدد.");

        public static Error AlreadyCompletedScheduleFrozen =>
            Error.Validation("SESSION_ALREADY_COMPLETED_SCHEDULE_FROZEN", "لا يمكن تعديل مواعيد حصة دراسية منتهية أو ملغاة.");

        public static Error CancellationReasonRequired =>
            Error.Validation("CANCELLATION_REASON_REQUIRED", "سبب الإلغاء إلزامي عند إلغاء الحصة الدراسية.");

        public static Error UnauthorizedWorkspaceAccess =>
            Error.Forbidden("UNAUTHORIZED_WORKSPACE_ACCESS", "مساحة العمل لا تملك صلاحية الوصول إلى هذه المجموعة.");
    }
}
