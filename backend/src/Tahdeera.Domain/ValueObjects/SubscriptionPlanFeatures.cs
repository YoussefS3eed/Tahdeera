namespace Tahdeera.Domain.ValueObjects;

public record SubscriptionPlanFeatures(
    bool StudentsAndGroups = true,
    bool NfcAttendance = true,
    bool PaymentsAndFees = true,
    bool WhatsappNotifications = true,
    bool ExamsAndQuestionBank = false,
    bool HomeworkAndLibrary = false,
    bool PerformanceReports = true,
    bool AiAssistant = false,
    bool ExcelExport = true,
    bool TeamPermissions = false);
