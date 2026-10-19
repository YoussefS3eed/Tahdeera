/**
 * Tahdeera Core Domain Models
 * Authoritative interfaces and regex validators from data-model.md
 */

import { AttendanceStatus, WorkspaceRole } from '../enums/domain.enums';

export interface UserProfile {
  id: string;
  fullName: string;
  email: string;
  phone: string;
  role: WorkspaceRole;
  workspaceId: string;
  workspaceName: string;
  avatarUrl?: string;
}

export interface Student {
  id: string;
  workspaceId: string;
  studentCode: string;           // Regex: ^ST-[0-9]{4,6}$
  fullName: string;
  guardianPhone: string;         // Regex: ^01[0125][0-9]{8}$
  studentPhone?: string;
  nfcCardUid?: string;           // Regex: ^[0-9A-Fa-f]{8,14}$
  currentBalance: number;
  isActive: boolean;
  notes?: string;
  createdAtUtc: string;
}

export interface ClassSession {
  id: string;
  workspaceId: string;
  groupId: string;
  groupName: string;
  subjectName: string;
  startsAt: string;              // ISO 8601 UTC
  endsAt: string;                // ISO 8601 UTC
  status: 'Scheduled' | 'Active' | 'Completed' | 'Cancelled';
}

export interface AttendanceRecord {
  id: string;
  sessionId: string;
  studentId: string;
  studentName: string;
  studentCode: string;
  status: AttendanceStatus;
  markedAtUtc: string;
  isAutomaticEvaluation: boolean;
  isLocked: boolean;             // True if locked by teacher or manual edit
  notes?: string;
}

/**
 * Domain Validation Regular Expressions
 */
export const VALIDATION_REGEX = {
  STUDENT_CODE: /^ST-[0-9]{4,6}$/,
  EGYPTIAN_PHONE: /^01[0125][0-9]{8}$/,
  NFC_CARD_UID: /^[0-9A-Fa-f]{8,14}$/,
  OPERATION_ID: /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i,
  TIMESTAMP_UTC: /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,3})?Z$/
} as const;
