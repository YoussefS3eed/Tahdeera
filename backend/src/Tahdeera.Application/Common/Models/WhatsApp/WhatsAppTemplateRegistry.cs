namespace Tahdeera.Application.Common.Models.WhatsApp;

public sealed record WhatsAppTemplateDefinition(
    string TemplateName,
    string Category,
    IReadOnlyList<string> SupportedLanguages,
    IReadOnlyList<string> Placeholders,
    string PreviewAr,
    string PreviewEn);

public static class WhatsAppTemplateRegistry
{
    public static readonly IReadOnlyList<string> AllowedPlaceholdersAr = new[]
    {
        "{{اسم_الطالب}}",
        "{{اسم_السنتر}}",
        "{{اسم_المجموعة}}",
        "{{التاريخ}}",
        "{{وقت_الحضور}}",
        "{{درجة_الامتحان}}"
    };

    public static readonly IReadOnlyList<string> AllowedPlaceholdersEn = new[]
    {
        "{{student_name}}",
        "{{workspace_name}}",
        "{{group_name}}",
        "{{session_date}}",
        "{{check_in_time}}",
        "{{exam_score}}"
    };

    public static readonly IReadOnlyDictionary<string, WhatsAppTemplateDefinition> Catalog =
        new Dictionary<string, WhatsAppTemplateDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["attendance_check_in_v1"] = new(
                "attendance_check_in_v1",
                "UTILITY",
                new[] { "ar", "en" },
                new[] { "{{student_name}}", "{{session_name}}", "{{time}}", "{{workspace_name}}" },
                "تم تسجيل حضور الطالب {{اسم_الطالب}} في سنتر {{اسم_السنتر}} في تمام الساعة {{وقت_الحضور}} بتاريخ {{التاريخ}}.",
                "Student {{student_name}} checked in at {{workspace_name}} at {{check_in_time}} on {{session_date}}."),

            ["absence_alert_v1"] = new(
                "absence_alert_v1",
                "UTILITY",
                new[] { "ar", "en" },
                new[] { "{{student_name}}", "{{session_name}}", "{{group_name}}", "{{workspace_name}}" },
                "تنبيه: تغيب الطالب {{اسم_الطالب}} عن حصة مجموعة {{اسم_المجموعة}} في سنتر {{اسم_السنتر}} بتاريخ {{التاريخ}}.",
                "Notice: Student {{student_name}} was absent from group {{group_name}} at {{workspace_name}} on {{session_date}}."),

            ["attendance_correction_v1"] = new(
                "attendance_correction_v1",
                "UTILITY",
                new[] { "ar", "en" },
                new[] { "{{student_name}}", "{{session_name}}", "{{group_name}}", "{{workspace_name}}" },
                "إشعار تعديل حضور: تم تعديل حالة حضور الطالب {{اسم_الطالب}} في سنتر {{اسم_السنتر}} بتاريخ {{التاريخ}}.",
                "Attendance update: Status for student {{student_name}} at {{workspace_name}} was updated on {{session_date}}."),

            ["exam_result_v1"] = new(
                "exam_result_v1",
                "UTILITY",
                new[] { "ar", "en" },
                new[] { "{{student_name}}", "{{group_name}}", "{{exam_score}}", "{{workspace_name}}" },
                "درجة امتحان الطالب {{اسم_الطالب}} في مجموعة {{اسم_المجموعة}}: {{درجة_الامتحان}} في سنتر {{اسم_السنتر}}.",
                "Exam result for student {{student_name}} in group {{group_name}}: {{exam_score}} at {{workspace_name}}."),

            ["weekly_parent_report_v1"] = new(
                "weekly_parent_report_v1",
                "UTILITY",
                new[] { "ar", "en" },
                new[] { "{{student_name}}", "{{workspace_name}}", "{{group_name}}" },
                "التقرير الأسبوعي للطالب {{اسم_الطالب}} في سنتر {{اسم_السنتر}} لمجموعة {{اسم_المجموعة}}.",
                "Weekly parent report for student {{student_name}} at {{workspace_name}} in group {{group_name}}."),

            ["payment_receipt_v1"] = new(
                "payment_receipt_v1",
                "UTILITY",
                new[] { "ar", "en" },
                new[] { "{{student_name}}", "{{workspace_name}}", "{{group_name}}" },
                "إيصال سداد: تم استلام مصاريف الطالب {{اسم_الطالب}} في سنتر {{اسم_السنتر}}.",
                "Payment receipt: Received fees for student {{student_name}} at {{workspace_name}}.")
        };

    public static bool IsValidPlaceholder(string placeholder) =>
        AllowedPlaceholdersAr.Contains(placeholder, StringComparer.Ordinal) ||
        AllowedPlaceholdersEn.Contains(placeholder, StringComparer.OrdinalIgnoreCase);
}
