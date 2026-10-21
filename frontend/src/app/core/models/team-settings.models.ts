export type WorkspaceStaffRole = 'Teacher' | 'Assistant' | 'Reception' | 'Accountant';
export type WorkspaceRole = WorkspaceStaffRole | 'Owner' | 'Admin';

export interface WorkspaceStaffMember {
  id: string;
  name: string;
  displayName?: string;
  username: string;
  email: string;
  phone: string;
  role: WorkspaceRole;
  isActive: boolean;
  status?: 'Active' | 'Deactivated';
  lastActiveUtc?: string;
  createdAt?: string;
  createdAtUtc: string;
  initialPasswordCleartext?: string;
}

export interface CreateStaffMemberRequest {
  displayName: string;
  username: string;
  email: string;
  phone: string;                      // Regex: ^01[0125][0-9]{8}$
  role: WorkspaceStaffRole;
  password: string;                   // Min 8 chars
}

export interface WorkspaceCenterProfile {
  centerNameAr: string;
  centerNameEn?: string;
  centerPhone: string;
  address: string;
  governorate: string;
  taxId?: string;
}

export interface WorkspaceAttendancePolicies {
  lateThresholdMinutes: number;
  autoAbsentThresholdMinutes: number;
  autoSendAbsenceWhatsApp: boolean;
  autoSendCheckInWhatsApp: boolean;
}

export interface WorkspaceSettings {
  workspaceId: string;
  centerName: string;
  teacherName: string;
  phone: string;
  address: string;
  defaultLateGraceMinutes: number;   // 10, 15, 30, 45, 60
  weeklyHolidays: number[];          // 5 = Friday, 6 = Saturday
  notifyOnSubscriptionWarning: boolean;
  notifyOnWhatsappFailure: boolean;

  // Rich UI nested structures
  profile?: WorkspaceCenterProfile;
  attendancePolicies?: WorkspaceAttendancePolicies;
}

export interface ReadOnlySubscriptionView {
  planName: string;                   // E.g. "Pro"
  packageNameAr?: string;
  status: 'Active' | 'ExpiringSoon' | 'Expired';
  startDate: string;
  endDate: string;
  expiresAt?: string;
  remainingDays: number;
  supportHotline1: string;            // E.g. "01272785618"
  supportHotline2: string;            // E.g. "01019482687"
  activeFeatures: string[];
  activeStudentsCount?: number;
  maxStudentsLimit?: number;
  currentStaffCount?: number;
  maxStaffLimit?: number;
  packagePlan?: string;
}
