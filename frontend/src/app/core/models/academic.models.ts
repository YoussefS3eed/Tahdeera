/**
 * Tahdeera Academic Domain Models & Type Contracts
 * Authoritative types for Milestone F3: SCR-05 through SCR-11
 * Reference: specs/010-f3-students-nfc-attendance/data-model.md
 */

export type GradeLevel =
  | 'Primary1' | 'Primary2' | 'Primary3' | 'Primary4' | 'Primary5' | 'Primary6'
  | 'Prep1' | 'Prep2' | 'Prep3'
  | 'Secondary1' | 'Secondary2' | 'Secondary3'
  | 'Grade1Secondary' | 'Grade2Secondary' | 'Grade3Secondary'
  | 'Grade1Prep' | 'Grade2Prep' | 'Grade3Prep';

export const GRADE_LABELS: Record<string, { ar: string; en: string }> = {
  Primary1: { ar: 'الأول الابتدائي', en: '1st Primary' },
  Primary2: { ar: 'الثاني الابتدائي', en: '2nd Primary' },
  Primary3: { ar: 'الثالث الابتدائي', en: '3rd Primary' },
  Primary4: { ar: 'الرابع الابتدائي', en: '4th Primary' },
  Primary5: { ar: 'الخامس الابتدائي', en: '5th Primary' },
  Primary6: { ar: 'السادس الابتدائي', en: '6th Primary' },
  Prep1: { ar: 'الأول الإعدادي', en: '1st Prep' },
  Prep2: { ar: 'الثاني الإعدادي', en: '2nd Prep' },
  Prep3: { ar: 'الثالث الإعدادي', en: '3rd Prep' },
  Secondary1: { ar: 'الأول الثانوي', en: '1st Secondary' },
  Secondary2: { ar: 'الثاني الثانوي', en: '2nd Secondary' },
  Secondary3: { ar: 'الثالث الثانوي', en: '3rd Secondary' },
  Grade1Secondary: { ar: 'الأول الثانوي', en: '1st Secondary' },
  Grade2Secondary: { ar: 'الثاني الثانوي', en: '2nd Secondary' },
  Grade3Secondary: { ar: 'الثالث الثانوي', en: '3rd Secondary' },
  Grade1Prep: { ar: 'الأول الإعدادي', en: '1st Prep' },
  Grade2Prep: { ar: 'الثاني الإعدادي', en: '2nd Prep' },
  Grade3Prep: { ar: 'الثالث الإعدادي', en: '3rd Prep' }
};

export type StudentAcademicStatus = 'Active' | 'Withdrawn' | 'Suspended';

export type AttendanceBadgeStatus =
  | 'Present'
  | 'Late'
  | 'Absent'
  | 'Excused'
  | 'Waiting'
  | 'Voided';

export type AttendanceSource = 'Nfc' | 'Qr' | 'Manual' | 'System' | 'WebNfc' | 'NfcCard' | 'QrCode' | 'ManualOverride';

export type SessionProgressStatus = 'Scheduled' | 'Running' | 'Completed' | 'Cancelled';

export type ScanMethod = 'UsbHidWedge' | 'WebNfc' | 'CameraQr' | 'ManualSearch';

export type ScanOutcomeStatus =
  | 'Present'
  | 'Late'
  | 'Duplicate'
  | 'WrongGroup'
  | 'Unregistered'
  | 'Revoked'
  | 'SessionClosed'
  | 'Error';

export type CardRevocationReason = 'Lost' | 'Damaged' | 'Stolen' | 'Replaced';

export interface AuditHistoryEntry {
  changedAt: string;
  changedByUserId: string;
  changedByUserName: string;
  previousStatus: AttendanceBadgeStatus;
  newStatus: AttendanceBadgeStatus;
  reason: string;
}

export interface StudentListItem {
  id: string;                         // UUID v4
  workspaceId?: string;
  studentCode: string;                // e.g. "ST-1042" (wrapped in .ltr-num)
  fullName: string;
  grade: GradeLevel | string;
  gradeLabelAr?: string;
  gradeLabelEn?: string;
  guardianName: string;
  guardianPhone: string;               // Egyptian Phone: ^01[0125][0-9]{8}$
  whatsappOptIn: boolean;
  activeNfcCardUid?: string | null;   // e.g. "04:A2:7C:19"
  qrIdentifierToken?: string | null;  // e.g. "TQR-WS01-ST1042-8F2B"
  enrolledGroupCount?: number;
  enrolledGroupIds?: string[];
  status: StudentAcademicStatus;
  withdrawnAt?: string | null;        // ISO 8601 string if Withdrawn
  attendanceRate: number;             // Percentage 0-100
  unpaidBalance?: number;              // In EGP
  tuitionBalance?: number;            // In EGP
  avatarUrl?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

export interface Student360Profile extends StudentListItem {
  attendanceRecords?: AttendanceRecordItem[];
  attendanceHistory?: AttendanceRecordItem[];
  financialSummary?: {
    totalRequired: number;
    totalPaid: number;
    balance: number;
    lastPaymentDate?: string | null;
  };
  financialLedger?: Array<{
    id: string;
    receiptNumber: string;
    amountPaid: number;
    billingMonth: string;
    paidAt: string;
    remainingBalance: number;
    receivedByStaffName: string;
  }>;
  examScores: Array<{
    id: string;
    examTitle: string;
    date?: string;
    examDate?: string;
    score: number;
    maxScore: number;
    percentage?: number;
    gradeBadge?: string;
    classRank?: number;
    notes?: string;
  }>;
  homeworkTasks?: Array<{
    id: string;
    title: string;
    dueDate: string;
    status: 'Submitted' | 'Late' | 'Missing';
    grade?: string | null;
  }>;
  homeworkSubmissions?: Array<{
    id: string;
    assignmentTitle: string;
    submittedAt?: string | null;
    status: 'Completed' | 'Incomplete' | 'Late' | 'Missing';
    score?: number | null;
    maxScore?: number;
  }>;
  whatsappLogs?: Array<{
    id: string;
    templateName: string;
    dispatchedAt: string;
    status: 'Delivered' | 'Pending' | 'Failed';
  }>;
  whatsappNotificationLogs?: Array<{
    id: string;
    messageBody: string;
    sentAtUtc: string;
    deliveryStatus: 'Delivered' | 'Pending' | 'Failed';
  }>;
  weeklyScorecard?: {
    weekRange: string;
    attendancePct: number;
    homeworkPct: number;
    quizAverage: number;
    teacherNotes: string;
  };
}

export interface StudyGroupItem {
  id: string;
  workspaceId?: string;
  name: string;
  grade: GradeLevel | string;
  gradeLabelAr?: string;
  gradeLabelEn?: string;
  subject: string;
  teacherUserId?: string;
  teacherName?: string;
  teacherInChargeName?: string;
  delegatedAssistantUserId?: string | null;
  delegatedAssistantStaffId?: string | null;
  delegatedAssistantName?: string | null;
  defaultMonthlyFee: number;
  lateGraceMinutes: number;
  enrolledCount?: number;
  currentEnrollmentCount?: number;
  maxCapacity: number;
  scheduleDescription?: string;
  weeklyScheduleSummary?: string;
}

export interface ClassSessionItem {
  id: string;
  workspaceId?: string;
  groupId: string;
  groupName: string;
  grade?: string;
  subject?: string;
  startsAt: string;              // ISO 8601 UTC
  endsAt: string;                // ISO 8601 UTC
  lateGraceMinutes: number;
  status: SessionProgressStatus;
  roomName?: string | null;
  totalRosterCount?: number;
  presentCount: number;
  lateCount: number;
  absentCount: number;
  excusedCount?: number;
  waitingCount?: number;
  voidedCount?: number;
  isCancelled?: boolean;
  cancellationReason?: string | null;
}

export interface AttendanceRecordItem {
  id: string;
  workspaceId?: string;
  sessionId: string;
  studentId: string;
  studentCode: string;
  studentName: string;
  status: AttendanceBadgeStatus;
  source: AttendanceSource | string;
  checkInAt?: string | null;
  scannedAt?: string | null;
  autoEvaluationLocked: boolean;
  isLocked?: boolean;
  overrideReason?: string | null;
  justificationReason?: string | null;
  lockedByUserId?: string | null;
  lockedAtUtc?: string | null;
  voidedAtUtc?: string | null;
  voidReason?: string | null;
  isSubstitute?: boolean;
  whatsappNoticeStatus?: 'Delivered' | 'Pending' | 'Failed' | 'NotSent';
  whatsappNotificationStatus?: 'Sent' | 'Queued' | 'Failed' | 'Cancelled' | string;
  history?: AuditHistoryEntry[];
}

export interface CredentialScanResult {
  outcome: ScanOutcomeStatus;
  recordId?: string | null;
  student?: {
    id: string;
    studentCode: string;
    fullName: string;
    guardianPhone: string;
    whatsappOptIn: boolean;
    photoUrl?: string | null;
  } | null;
  scannedAtUtc: string;
  lateMinutes?: number;
  originalGroupId?: string;
  originalGroupName?: string;
  soundToPlay: 'PresentChime' | 'LateChime' | 'DuplicateTone' | 'ErrorBuzz';
  messageAr: string;
  messageEn: string;
}

export interface NfcCardItem {
  id?: string;
  workspaceId?: string;
  cardUid: string;               // e.g. "04:A2:7C:19"
  label?: string;
  assignedStudentId?: string | null;
  assignedStudentName?: string | null;
  assignedStudentCode?: string | null;
  issuedAt: string;
  status: 'Active' | 'Revoked' | 'Unassigned';
  revocationReason?: string | null;
}

export interface SyncBatchItemPayload {
  operationId: string;
  sessionId: string;
  studentId: string;
  identifierUsed: string;
  scannedAtUtc: string;
  source: string;
}

export interface SyncBatchRequest {
  workspaceId: string;
  items: SyncBatchItemPayload[];
}

export interface SyncBatchResultItem {
  operationId: string;
  status: 'Synced' | 'Conflict' | 'Failed';
  conflictReason?: string;
  errorMessage?: string;
}

export interface SyncBatchResponse {
  batchId: string;
  processedAtUtc: string;
  syncedCount: number;
  conflictCount: number;
  results: SyncBatchResultItem[];
}

export type { LocalAttendanceRow } from '../services/tahdeera-local-db';

export interface SyncConflictRecord {
  localId: number;
  operationId: string;
  studentId: string;
  studentCode: string;
  studentName: string;
  sessionId: string;
  sessionTitle: string;
  scannedAtUtc: string;
  conflictReason: 'SessionCancelledOnline' | 'StudentWithdrawn' | 'VersionMismatch' | string;
  cancellationReason?: string;
  status: 'Conflict' | 'Resolved';
}

export const ACADEMIC_VALIDATION_REGEX = {
  STUDENT_CODE: /^ST-[0-9]{4,6}$/,
  EGYPTIAN_PHONE: /^01[0125][0-9]{8}$/,
  NFC_CARD_UID: /^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2})+$|^[0-9A-Fa-f]{8,14}$/,
  SECRET_PIN: /^[0-9]{4}$/,
  OPERATION_ID: /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i,
  TIMESTAMP_UTC: /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,3})?Z$/
} as const;
