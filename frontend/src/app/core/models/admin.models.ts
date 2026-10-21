export type PlatformAccountStatus = 'Active' | 'Suspended';
export type PlatformSubscriptionStatus = 'Active' | 'ExpiringSoon' | 'Expired';
export type WorkspaceType = 'Teacher' | 'Center';
export type PackageTier = 'basic' | 'pro' | 'center';
export type SubscriptionPeriodPreset = '1D' | '7D' | '30D' | '90D' | '365D' | 'Custom';
export type PlatformPaymentMethod = 'BankTransfer' | 'InstaPay' | 'VodafoneCash' | 'Cash';
export type PlatformPaymentStatus = 'Completed' | 'Reversed';

export type AdminAuditAction = string;

export interface GranularFeatureFlags {
  studentsAndGroups: boolean;
  nfcAttendance: boolean;
  paymentsAndFees: boolean;
  whatsappNotifications: boolean;
  examsAndQuestionBank: boolean;
  homeworkAndLibrary: boolean;
  aiAssistant: boolean;
  excelExport: boolean;
  maxStudents: number;
  maxStaff: number;
}

export interface AdminWorkspaceSummary {
  id: string;
  code: string;
  name: string;
  type: WorkspaceType;
  ownerName: string;
  ownerPhone: string;
  ownerEmail: string;
  packageTier: PackageTier;
  studentCount: number;
  activeGroupCount: number;
  subscriptionStatus?: PlatformSubscriptionStatus;
  accountStatus?: PlatformAccountStatus;
  status?: 'Active' | 'Suspended';
  subscriptionExpiresAt: string;
  daysRemaining?: number;
  subscriptionDaysRemaining?: number;
  contractedPrice?: number;
  agreedPrice?: number;
  maxStudentsQuota?: number;
  paidAmount?: number;
  manualSuspendedAt?: string | null;
  suspensionReason?: string | null;
  featureOverrides?: Partial<GranularFeatureFlags> | null;
}

export interface PlatformOverviewKpis {
  totalAccounts?: number;
  activeAccounts?: number;
  expiredAccounts?: number;
  suspendedAccounts?: number;
  expiringSoonCount?: number;
  aggregateStudentsCount?: number;
  totalContractedRevenue?: number;
  totalCollectedRevenue?: number;
  outstandingReceivables?: number;
  activeCentersCount?: number;
  suspendedCentersCount?: number;
  activeStudentsCount?: number;
  monthlyRevenueEgp?: number;
  expiringIn7DaysCount?: number;
}

export interface RenewalWatchlistItem {
  workspaceId: string;
  workspaceName: string;
  workspaceCode?: string;
  ownerName?: string;
  type?: WorkspaceType;
  expiryDate?: string;
  expiresAt?: string;
  daysRemaining: number;
  packageTier?: PackageTier;
  agreedPrice: number;
}

export interface PackageTemplate {
  id: string;
  tier?: PackageTier;
  name?: string;
  description?: string;
  priceEgp?: number;
  nameAr?: string;
  nameEn?: string;
  priceMonthly?: number;
  priceYearly?: number;
  maxStudents?: number;
  maxStaff?: number;
  features: GranularFeatureFlags;
}

export interface PlatformSubscriptionPayment {
  id: string;
  receiptNumber?: string;
  transactionRef?: string;
  workspaceId: string;
  workspaceName: string;
  packageTier?: PackageTier;
  amount: number;
  method: PlatformPaymentMethod;
  referenceNumber: string;
  status: PlatformPaymentStatus;
  createdAt?: string;
  paidAt?: string;
  reversalReason?: string;
  reversedAt?: string;
}

export interface WhatsAppProviderConfig {
  phoneNumberId: string;
  wabaId: string;
  accessToken: string;
  appSecret?: string;
  webhookVerifyToken: string;
  status?: string;
  qualityRating?: string;
}

export interface WhatsAppTestPingResult {
  status: 'Connected' | 'Failed';
  verifiedName?: string;
  qualityRating?: 'Green' | 'Yellow' | 'Red';
  messagingTier?: string;
  pingDurationMs?: number;
}

export interface WhatsAppPlatformStats {
  totalOutboxCount?: number;
  sentCount?: number;
  deliveredCount?: number;
  failedTransientCount?: number;
  failedPermanentCount?: number;
  totalSent?: number;
  delivered?: number;
  read?: number;
  failed?: number;
}

export interface PlatformGlobalSettings {
  brandName?: string;
  platformName?: string;
  tagline?: string;
  taglineAr?: string;
  taglineEn?: string;
  primarySupportPhone?: string;
  secondarySupportPhone?: string;
  primarySupportHotline?: string;
  secondarySupportHotline?: string;
  supportEmail?: string;
  defaultCurrency?: string;
  backupSchedule?: string;
  auditRetentionDays?: number;
  auditLogRetentionDays?: number;
  allowNewRegistrations?: boolean;
  maintenanceMode?: boolean;
}

export interface AdminAuditLogEntry {
  id: string;
  timestamp?: string;
  createdAt?: string;
  actor?: string;
  adminId?: string;
  adminName?: string;
  action: AdminAuditAction;
  targetType?: string;
  targetId?: string;
  targetName?: string;
  targetWorkspaceId?: string;
  targetWorkspaceName?: string;
  description?: string;
  ipAddress?: string;
  beforeState?: Record<string, any> | null;
  afterState?: Record<string, any> | null;
}
