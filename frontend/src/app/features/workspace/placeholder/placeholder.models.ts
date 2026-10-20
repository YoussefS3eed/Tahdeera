import { WorkspaceRole } from '../../../core/models/auth.models';

export type TargetMilestone = 'Milestone F3' | 'Milestone F4';

export interface DeferredTabMetadata {
  routePath: string;
  iconName: string;
  titleAr: string;
  titleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  targetMilestone: TargetMilestone;
  requiredRoles?: WorkspaceRole[];
}

export const DEFERRED_WORKSPACE_TABS: Record<string, DeferredTabMetadata> = {
  'nfc-station': {
    routePath: 'nfc-station',
    iconName: 'contactless',
    titleAr: 'محطة الحضور المباشر NFC',
    titleEn: 'Live NFC Attendance Station',
    descriptionAr: 'تسجيل فوري لحضور الطلاب عبر قارئ NFC ACR122U مع استجابة صوتية ومرئية فورية وتحديثات تلقائية.',
    descriptionEn: 'Instant student attendance recording via NFC ACR122U with audio-visual feedback and real-time updates.',
    targetMilestone: 'Milestone F3'
  },
  'attendance': {
    routePath: 'attendance',
    iconName: 'fact_check',
    titleAr: 'كشف الحضور والغياب اليومي',
    titleEn: 'Daily Attendance Roster',
    descriptionAr: 'سجل تفصيلي لحضور الطلاب في الحصص اليومية، مع إمكانية التعديل اليدوي وإعادة إرسال إشعارات الواتساب.',
    descriptionEn: 'Detailed attendance records with manual adjustments and WhatsApp notification re-dispatching.',
    targetMilestone: 'Milestone F3'
  },
  'students': {
    routePath: 'students',
    iconName: 'groups',
    titleAr: 'سجل وملفات الطلاب',
    titleEn: 'Student Directory & Profiles',
    descriptionAr: 'إدارة ملفات الطلاب، بيانات أولياء الأمور، ربط البطاقات الذكية، ومتابعة التاريخ الأكاديمي والمالي.',
    descriptionEn: 'Comprehensive student management, parent contacts, smart card linking, and academic history.',
    targetMilestone: 'Milestone F3'
  },
  'cards': {
    routePath: 'cards',
    iconName: 'credit_card',
    titleAr: 'بطاقات الحضور الذكية NFC',
    titleEn: 'NFC Smart Cards Management',
    descriptionAr: 'ربط بطاقات الطلاب بقارئ USB، استبدال البطاقات المفقودة، وطباعة الأكواد التعريفية للطلاب.',
    descriptionEn: 'Student NFC card assignment, lost card replacement, and student QR/code printing.',
    targetMilestone: 'Milestone F3'
  },
  'groups': {
    routePath: 'groups',
    iconName: 'class',
    titleAr: 'المجموعات والصفوف الدراسية',
    titleEn: 'Student Groups & Classes',
    descriptionAr: 'إنشاء وإدارة المجموعات الدراسية، تحديد المواعيد والسعة، وتعيين المساعدين المسؤولين.',
    descriptionEn: 'Manage study groups, schedules, capacity limits, and assistant teacher delegation.',
    targetMilestone: 'Milestone F3'
  },
  'schedule': {
    routePath: 'schedule',
    iconName: 'calendar_today',
    titleAr: 'الجدول الدراسي الأسبوعي',
    titleEn: 'Weekly Class Schedule',
    descriptionAr: 'عرض مرئي لجدول الحصص الأسبوعي، إدارة تضارب المواعيد، وإلغاء الحصص مع التحديث التلقائي.',
    descriptionEn: 'Visual weekly schedule planner, conflict resolution, and automated cancellation workflows.',
    targetMilestone: 'Milestone F3'
  },
  'payments': {
    routePath: 'payments',
    iconName: 'payments',
    titleAr: 'سجل المصروفات والمدفوعات',
    titleEn: 'Tuition & Payments Ledger',
    descriptionAr: 'تحصيل المصروفات الدراسية، إصدار الإيصالات غير القابلة للتعديل، وسجل التدقيق المالي المحكم.',
    descriptionEn: 'Tuition collection, immutable payment receipts, reversal governance, and financial ledger.',
    targetMilestone: 'Milestone F4',
    requiredRoles: ['Owner', 'Admin']
  },
  'whatsapp': {
    routePath: 'whatsapp',
    iconName: 'chat',
    titleAr: 'مركز رسائل الواتساب',
    titleEn: 'WhatsApp Notification Center',
    descriptionAr: 'قوالب رسائل الحضور والغياب، سجل تسليم الإشعارات الفوري، وإدارة اشتراك أولياء الأمور.',
    descriptionEn: 'Attendance and absence message templates, delivery logs, and guardian opt-in management.',
    targetMilestone: 'Milestone F4'
  },
  'lms': {
    routePath: 'lms',
    iconName: 'menu_book',
    titleAr: 'المنصة التعليمية والواجبات',
    titleEn: 'LMS Modules & Homework',
    descriptionAr: 'بنك الأسئلة المعتمد، الامتحانات الإلكترونية المؤقتة، تصحيح الواجبات، والمكتبة الرقمية.',
    descriptionEn: 'Question bank, timed online exams, automated grading, assignments, and digital library.',
    targetMilestone: 'Milestone F4'
  },
  'reports': {
    routePath: 'reports',
    iconName: 'insights',
    titleAr: 'التقارير الأكاديمية والمالية',
    titleEn: 'Academic & Financial Reports',
    descriptionAr: '10 تقارير تحليلية معيارية لمعدلات الحضور، التحصيل المالي، ونمو الطلاب مع التصدير إلى Excel.',
    descriptionEn: '10 standard operational reports for attendance rates, finance, and Excel data export.',
    targetMilestone: 'Milestone F4'
  },
  'team': {
    routePath: 'team',
    iconName: 'badge',
    titleAr: 'فريق العمل والصلاحيات',
    titleEn: 'Staff & Team Permissions',
    descriptionAr: 'إدارة حسابات المساعدين وموظفي الاستقبال، تفويض صلاحيات التحضير، وتتبع سجل النشاط.',
    descriptionEn: 'Assistant and receptionist management, attendance delegation, and audit activity tracking.',
    targetMilestone: 'Milestone F4',
    requiredRoles: ['Owner', 'Admin']
  },
  'settings': {
    routePath: 'settings',
    iconName: 'settings',
    titleAr: 'إعدادات السنتر والنظام',
    titleEn: 'Center & System Settings',
    descriptionAr: 'تخصيص بيانات السنتر، فترات السماح بالتحضير، سياسات التأخير، وتفاصيل باقة الاشتراك.',
    descriptionEn: 'Center profile customization, late check-in grace minutes, and subscription tier settings.',
    targetMilestone: 'Milestone F4',
    requiredRoles: ['Owner', 'Admin']
  },
  'ai-assistant': {
    routePath: 'ai-assistant',
    iconName: 'smart_toy',
    titleAr: 'المساعد الذكي',
    titleEn: 'AI Educational Assistant',
    descriptionAr: 'استعلام ذكي باللغة الطبيعية عن حضور الطلاب، تحليل مستويات الالتزام، وتوليد مسودات الأسئلة.',
    descriptionEn: 'Natural language queries for student insights, attendance trends, and AI question drafting.',
    targetMilestone: 'Milestone F4'
  }
};
