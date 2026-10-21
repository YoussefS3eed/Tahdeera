import { PackageTier, PlatformPaymentMethod, WorkspaceType } from './admin.models';

export interface CreateWorkspaceRequest {
  name: string;
  type: WorkspaceType;
  packageTier: PackageTier;
  ownerName: string;
  ownerPhone: string;
  ownerEmail: string;
  agreedPrice: number;
  maxStudents: number;
  whatsappPhoneId?: string;
  whatsappAccessToken?: string;
}

export interface SuspendWorkspaceRequest {
  suspensionReason: string;
}

export interface RenewSubscriptionRequest {
  durationDays: number;
  agreedPrice: number;
  paymentStatus?: 'Paid' | 'Partial' | 'Unpaid';
  internalNotes?: string;
}

export interface RecordPlatformPaymentRequest {
  workspaceId: string;
  amount: number;
  method: PlatformPaymentMethod;
  referenceNumber: string;
  notes?: string;
}

export interface ReversePlatformPaymentRequest {
  reversalReason: string;
}

export interface UpdatePlatformSettingsRequest {
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
  auditRetentionDays?: number;
  auditLogRetentionDays?: number;
  allowNewRegistrations?: boolean;
  maintenanceMode?: boolean;
}
