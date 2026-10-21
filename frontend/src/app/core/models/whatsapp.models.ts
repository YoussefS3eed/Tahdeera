export type WhatsAppMessageType =
  | 'AttendanceCheckIn'
  | 'AbsenceAlert'
  | 'ExamResult'
  | 'WeeklyParentReport'
  | 'PaymentReceipt'
  | 'AttendanceCorrection';

export type WhatsAppDeliveryStatus =
  | 'Pending'
  | 'PublishedToBroker'
  | 'Processing'
  | 'Sent'
  | 'Delivered'
  | 'FailedTransient'
  | 'FailedPermanent'
  | 'Cancelled';

export interface WhatsAppTemplate {
  type: WhatsAppMessageType;
  titleAr: string;
  titleEn: string;
  rawText: string;
  availableTokens: string[];
  isEnabled: boolean;
  lastUpdatedUtc: string;
}

export interface WhatsAppMessageLog {
  id: string;                         // E.g. "MSG-2026-8821"
  workspaceId: string;
  studentId: string;
  studentName: string;
  guardianPhone: string;              // E.g. "01012345670"
  messageType: WhatsAppMessageType;
  groupName: string;
  dispatchedAtUtc: string;
  createdAt?: string;
  renderedText?: string;
  messageContent?: string;
  status: WhatsAppDeliveryStatus;
  bufferSecondsRemaining?: number;    // 30s countdown while Pending
  providerMessageId?: string;
  failureReason?: string;
  retryCount: number;
  idempotencyKey: string;             // E.g. "att_rec_104_Present"
}

export interface GuardianConsentRecord {
  studentId: string;
  studentName: string;
  studentCode: string;
  guardianName?: string;
  guardianPhone: string;
  whatsappOptIn: boolean;
  consentDate?: string;
  lastMessageAtUtc?: string;
}
