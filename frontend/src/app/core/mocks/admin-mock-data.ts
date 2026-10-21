import {
  AdminAuditLogEntry,
  AdminWorkspaceSummary,
  PackageTemplate,
  PackageTier,
  PlatformGlobalSettings,
  PlatformOverviewKpis,
  PlatformSubscriptionPayment,
  RenewalWatchlistItem,
  WhatsAppPlatformStats,
  WhatsAppProviderConfig
} from '../models/admin.models';

export const INITIAL_ADMIN_KPIS: PlatformOverviewKpis = {
  totalAccounts: 14,
  activeAccounts: 11,
  expiredAccounts: 2,
  suspendedAccounts: 1,
  expiringSoonCount: 3,
  aggregateStudentsCount: 3480,
  totalContractedRevenue: 142000,
  totalCollectedRevenue: 118500,
  outstandingReceivables: 23500
};

export const INITIAL_PACKAGE_TEMPLATES: PackageTemplate[] = [
  {
    id: 'basic',
    nameAr: 'الباقة الأساسية',
    nameEn: 'Basic Tier',
    priceMonthly: 350,
    priceYearly: 3500,
    maxStudents: 150,
    maxStaff: 1,
    features: {
      studentsAndGroups: true,
      nfcAttendance: false,
      paymentsAndFees: true,
      whatsappNotifications: false,
      examsAndQuestionBank: false,
      homeworkAndLibrary: false,
      aiAssistant: false,
      excelExport: false,
      maxStudents: 150,
      maxStaff: 1
    }
  },
  {
    id: 'pro',
    nameAr: 'باقة المحترفين',
    nameEn: 'Pro Tier',
    priceMonthly: 750,
    priceYearly: 7500,
    maxStudents: 500,
    maxStaff: 3,
    features: {
      studentsAndGroups: true,
      nfcAttendance: true,
      paymentsAndFees: true,
      whatsappNotifications: true,
      examsAndQuestionBank: true,
      homeworkAndLibrary: true,
      aiAssistant: false,
      excelExport: true,
      maxStudents: 500,
      maxStaff: 3
    }
  },
  {
    id: 'center',
    nameAr: 'باقة السناتر المتقدمة',
    nameEn: 'Center Enterprise',
    priceMonthly: 1800,
    priceYearly: 18000,
    maxStudents: 2000,
    maxStaff: 15,
    features: {
      studentsAndGroups: true,
      nfcAttendance: true,
      paymentsAndFees: true,
      whatsappNotifications: true,
      examsAndQuestionBank: true,
      homeworkAndLibrary: true,
      aiAssistant: true,
      excelExport: true,
      maxStudents: 2000,
      maxStaff: 15
    }
  }
];

export const INITIAL_ADMIN_WORKSPACES: AdminWorkspaceSummary[] = [
  {
    id: 'ws-01',
    code: 'AWAEL',
    name: 'سنتر الأوائل التعليمي',
    type: 'Center',
    ownerName: 'أ. محمود السيد',
    ownerPhone: '01019482687',
    ownerEmail: 'awael.center@tahdeera.app',
    packageTier: 'center',
    studentCount: 420,
    activeGroupCount: 18,
    subscriptionStatus: 'ExpiringSoon',
    accountStatus: 'Active',
    subscriptionExpiresAt: '2026-10-25T00:00:00Z',
    daysRemaining: 4,
    contractedPrice: 12000,
    paidAmount: 10000,
    featureOverrides: { nfcAttendance: true, whatsappNotifications: true }
  },
  {
    id: 'ws-02',
    code: 'NOOR',
    name: 'أكاديمية النور للغات',
    type: 'Center',
    ownerName: 'د. سارة عثمان',
    ownerPhone: '01272785618',
    ownerEmail: 'noor.academy@tahdeera.app',
    packageTier: 'center',
    studentCount: 650,
    activeGroupCount: 24,
    subscriptionStatus: 'Active',
    accountStatus: 'Active',
    subscriptionExpiresAt: '2026-11-20T00:00:00Z',
    daysRemaining: 30,
    contractedPrice: 18000,
    paidAmount: 18000
  },
  {
    id: 'ws-03',
    code: 'EADL',
    name: 'أ. إبراهيم عادل (لغة إنجليزية)',
    type: 'Teacher',
    ownerName: 'أ. إبراهيم عادل',
    ownerPhone: '01123456789',
    ownerEmail: 'ibrahim.english@tahdeera.app',
    packageTier: 'pro',
    studentCount: 380,
    activeGroupCount: 10,
    subscriptionStatus: 'ExpiringSoon',
    accountStatus: 'Active',
    subscriptionExpiresAt: '2026-10-27T00:00:00Z',
    daysRemaining: 6,
    contractedPrice: 4500,
    paidAmount: 4500
  },
  {
    id: 'ws-04',
    code: 'KHALIL',
    name: 'أ. حسام خليل (فيزياء ثانوية عامة)',
    type: 'Teacher',
    ownerName: 'أ. حسام خليل',
    ownerPhone: '01511223344',
    ownerEmail: 'hossam.physics@tahdeera.app',
    packageTier: 'pro',
    studentCount: 510,
    activeGroupCount: 12,
    subscriptionStatus: 'Active',
    accountStatus: 'Active',
    subscriptionExpiresAt: '2026-12-05T00:00:00Z',
    daysRemaining: 45,
    contractedPrice: 5000,
    paidAmount: 5000
  },
  {
    id: 'ws-suspended-1',
    code: 'ANDALUS',
    name: 'سنتر الأندلس للتدريب',
    type: 'Center',
    ownerName: 'أ. طارق فاروق',
    ownerPhone: '01099887766',
    ownerEmail: 'andalus@tahdeera.app',
    packageTier: 'center',
    studentCount: 290,
    activeGroupCount: 14,
    subscriptionStatus: 'Expired',
    accountStatus: 'Suspended',
    subscriptionExpiresAt: '2026-10-15T00:00:00Z',
    daysRemaining: 0,
    contractedPrice: 10000,
    paidAmount: 4000,
    manualSuspendedAt: '2026-10-21T14:30:00Z',
    suspensionReason: 'عدم سداد المستحقات الشهرية المتأخرة بعد الإنذار الثالث'
  },
  {
    id: 'ws-expired-1',
    code: 'ALFA',
    name: 'أكاديمية ألفا التعليمية',
    type: 'Center',
    ownerName: 'أ. وائل غنيم',
    ownerPhone: '01233445566',
    ownerEmail: 'alfa@tahdeera.app',
    packageTier: 'center',
    studentCount: 310,
    activeGroupCount: 11,
    subscriptionStatus: 'Expired',
    accountStatus: 'Active',
    subscriptionExpiresAt: '2026-10-18T00:00:00Z',
    daysRemaining: 0,
    contractedPrice: 8500,
    paidAmount: 8500
  }
];

export const INITIAL_PAYMENTS_LEDGER: PlatformSubscriptionPayment[] = [
  {
    id: 'tx-2026-0001',
    transactionRef: 'TX-2026-0001',
    workspaceId: 'ws-01',
    workspaceName: 'سنتر الأوائل التعليمي',
    packageTier: 'center',
    amount: 10000,
    method: 'VodafoneCash',
    referenceNumber: 'VF-998822',
    status: 'Completed',
    createdAt: '2026-10-21T10:15:00Z'
  },
  {
    id: 'tx-2026-0002',
    transactionRef: 'TX-2026-0002',
    workspaceId: 'ws-02',
    workspaceName: 'أكاديمية النور للغات',
    packageTier: 'center',
    amount: 18000,
    method: 'BankTransfer',
    referenceNumber: 'CIB-554120',
    status: 'Completed',
    createdAt: '2026-10-20T14:00:00Z'
  },
  {
    id: 'tx-2026-0003',
    transactionRef: 'TX-2026-0003',
    workspaceId: 'ws-03',
    workspaceName: 'أ. إبراهيم عادل (لغة إنجليزية)',
    packageTier: 'pro',
    amount: 4500,
    method: 'InstaPay',
    referenceNumber: 'IPN-776655',
    status: 'Completed',
    createdAt: '2026-10-19T11:30:00Z'
  },
  {
    id: 'tx-2026-0004',
    transactionRef: 'TX-2026-0004',
    workspaceId: 'ws-suspended-1',
    workspaceName: 'سنتر الأندلس للتدريب',
    packageTier: 'center',
    amount: 4000,
    method: 'Cash',
    referenceNumber: 'CSH-10492',
    status: 'Reversed',
    createdAt: '2026-10-15T09:00:00Z',
    reversalReason: 'خطأ إيداع بالدفتر النقدي وتم عمل قيد عكسي بالمعاملة رقم REV-2026-0004',
    reversedAt: '2026-10-16T12:00:00Z'
  }
];

export const INITIAL_WHATSAPP_CONFIG: WhatsAppProviderConfig = {
  phoneNumberId: '109827364519283',
  wabaId: '992817263548192',
  accessToken: 'EAAQZBP_SYSTEM_USER_PERMANENT_TOKEN_SECRET_98124',
  webhookVerifyToken: 'tahdeera_webhook_secret_verification_token_2026'
};

export const INITIAL_WHATSAPP_STATS: WhatsAppPlatformStats = {
  totalOutboxCount: 48210,
  sentCount: 47910,
  deliveredCount: 47120,
  failedTransientCount: 210,
  failedPermanentCount: 80
};

export const INITIAL_GLOBAL_SETTINGS: PlatformGlobalSettings = {
  brandName: 'تحضيره - Tahdeera',
  taglineAr: 'منظومة الحضور الذكي وإدارة السناتر التعليمية',
  taglineEn: 'Smart Attendance & Educational Center Management',
  primarySupportPhone: '01272785618',
  secondarySupportPhone: '01019482687',
  defaultCurrency: 'EGP',
  backupSchedule: 'Hourly WAL + Daily Snapshot (03:00 UTC)',
  auditRetentionDays: 365
};

export const INITIAL_ADMIN_AUDIT_LOGS: AdminAuditLogEntry[] = [
  {
    id: 'aud-109',
    timestamp: '2026-10-21T14:30:00Z',
    actor: 'م. أحمد الشريف',
    action: 'SubscriptionRenewed',
    targetWorkspaceId: 'ws-01',
    targetWorkspaceName: 'سنتر الأوائل التعليمي',
    description: 'تمديد اشتراك السنتر لمدة ٣٠ يوماً بقيمة 12,000 ج.م',
    ipAddress: '197.38.45.12',
    beforeState: { subscriptionExpiresAt: '2026-10-25T00:00:00Z', daysRemaining: 4 },
    afterState: { subscriptionExpiresAt: '2026-11-24T00:00:00Z', daysRemaining: 34 }
  },
  {
    id: 'aud-108',
    timestamp: '2026-10-21T14:00:00Z',
    actor: 'م. أحمد الشريف',
    action: 'WorkspaceSuspended',
    targetWorkspaceId: 'ws-suspended-1',
    targetWorkspaceName: 'سنتر الأندلس للتدريب',
    description: 'إيقاف السنتر إدارياً لمخالفة سداد المستحقات المتأخرة (DEC-08)',
    ipAddress: '197.38.45.12',
    beforeState: { accountStatus: 'Active' },
    afterState: { accountStatus: 'Suspended', suspensionReason: 'عدم سداد المستحقات' }
  },
  {
    id: 'aud-107',
    timestamp: '2026-10-21T10:15:00Z',
    actor: 'م. أحمد الشريف',
    action: 'PaymentRecorded',
    targetWorkspaceId: 'ws-01',
    targetWorkspaceName: 'سنتر الأوائل التعليمي',
    description: 'تسجيل دفعة اشتراك بقيمة 10,000 ج.م عبر فودافون كاش',
    ipAddress: '197.38.45.12',
    beforeState: null,
    afterState: { txRef: 'TX-2026-0001', amount: 10000, method: 'VodafoneCash' }
  }
];

export const MOCK_ADMIN_WORKSPACES: AdminWorkspaceSummary[] = INITIAL_ADMIN_WORKSPACES.map(w => ({
  ...w,
  status: (w.accountStatus || 'Active') as 'Active' | 'Suspended',
  subscriptionDaysRemaining: w.daysRemaining,
  agreedPrice: w.contractedPrice,
  maxStudentsQuota: 500
}));

export const MOCK_PLATFORM_OVERVIEW_KPIS: PlatformOverviewKpis = {
  ...INITIAL_ADMIN_KPIS,
  activeCentersCount: INITIAL_ADMIN_KPIS.activeAccounts,
  suspendedCentersCount: INITIAL_ADMIN_KPIS.suspendedAccounts,
  activeStudentsCount: INITIAL_ADMIN_KPIS.aggregateStudentsCount,
  monthlyRevenueEgp: INITIAL_ADMIN_KPIS.totalContractedRevenue,
  expiringIn7DaysCount: INITIAL_ADMIN_KPIS.expiringSoonCount
};

export const MOCK_RENEWAL_WATCHLIST: RenewalWatchlistItem[] = INITIAL_ADMIN_WORKSPACES.slice(0, 3).map(w => ({
  workspaceId: w.id,
  workspaceName: w.name,
  ownerName: w.ownerName,
  expiresAt: w.subscriptionExpiresAt,
  daysRemaining: w.daysRemaining ?? 3,
  agreedPrice: w.contractedPrice ?? 500
}));

export const MOCK_PACKAGE_TEMPLATES: PackageTemplate[] = INITIAL_PACKAGE_TEMPLATES.map(p => ({
  ...p,
  tier: p.id as PackageTier,
  name: p.nameAr,
  priceEgp: p.priceMonthly,
  description: p.nameEn + ' package tier'
}));

export const MOCK_SUBSCRIPTION_PAYMENTS: PlatformSubscriptionPayment[] = INITIAL_PAYMENTS_LEDGER.map(p => ({
  ...p,
  receiptNumber: p.transactionRef,
  paidAt: p.createdAt
}));

export const MOCK_WHATSAPP_CONFIG: WhatsAppProviderConfig = {
  ...INITIAL_WHATSAPP_CONFIG,
  appSecret: 'mock_app_secret',
  status: 'Connected',
  qualityRating: 'Green'
};

export const MOCK_WHATSAPP_STATS: WhatsAppPlatformStats = {
  ...INITIAL_WHATSAPP_STATS,
  totalSent: INITIAL_WHATSAPP_STATS.totalOutboxCount,
  delivered: INITIAL_WHATSAPP_STATS.deliveredCount,
  read: Math.floor((INITIAL_WHATSAPP_STATS.deliveredCount ?? 0) * 0.9),
  failed: INITIAL_WHATSAPP_STATS.failedPermanentCount
};

export const MOCK_GLOBAL_SETTINGS: PlatformGlobalSettings = {
  ...INITIAL_GLOBAL_SETTINGS,
  platformName: INITIAL_GLOBAL_SETTINGS.brandName,
  tagline: INITIAL_GLOBAL_SETTINGS.taglineAr,
  primarySupportHotline: INITIAL_GLOBAL_SETTINGS.primarySupportPhone,
  secondarySupportHotline: INITIAL_GLOBAL_SETTINGS.secondarySupportPhone,
  supportEmail: 'support@tahdeera.app',
  allowNewRegistrations: true,
  maintenanceMode: false,
  auditLogRetentionDays: INITIAL_GLOBAL_SETTINGS.auditRetentionDays
};

export const MOCK_ADMIN_AUDIT_LOGS: AdminAuditLogEntry[] = INITIAL_ADMIN_AUDIT_LOGS.map(l => ({
  ...l,
  createdAt: l.timestamp,
  adminName: l.actor,
  targetName: l.targetWorkspaceName,
  targetType: 'Workspace'
}));
