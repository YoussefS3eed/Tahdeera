export interface KpiTotalStudents {
  totalCount: number;
  activeCount: number;
  inactiveCount: number;
}

export interface KpiPresentToday {
  presentCount: number;
  attendancePercentage: number;
  totalScheduledRecords: number;
}

export interface KpiAbsentToday {
  absentCount: number;
  unexcusedCount: number;
  excusedCount: number;
}

export interface KpiUnpaidTuition {
  totalOverdueAmount: number;
  currency: string;
  unpaidStudentCount: number;
}

export interface DashboardKpiSummary {
  totalStudents: KpiTotalStudents;
  presentToday: KpiPresentToday;
  absentToday: KpiAbsentToday;
  // STRICT CONF-09 RULE: null or undefined for Assistant / Receptionist roles
  unpaidTuition?: KpiUnpaidTuition | null;
}

export interface RenewalNoticeBanner {
  showBanner: boolean;
  daysRemaining: number;
  expiryDate: string;
  supportHotlines: string[];
}

export type SessionProgressStatus = 'Waiting' | 'InProgress' | 'Completed' | 'PendingClosure';

export interface TodaySessionItem {
  id: string;
  groupName: string;
  subject: string;
  gradeLevel: string;
  scheduledStart: string;
  scheduledEnd: string;
  status: SessionProgressStatus;
  statusBadgeLabel: string;
  presentCount: number;
  rosterCapacity: number;
  actionRoute: string;
  actionLabel: string;
}

export type ScanMethod = 'NFC' | 'Manual';
export type AttendanceBadgeStatus = 'Present' | 'Late' | 'Absent' | 'Excused';
export type WhatsAppDeliveryStatus = 'Delivered' | 'Pending' | 'Failed';

export interface RecentAttendanceItem {
  id: string;
  studentId: string;
  studentCode: string;
  studentName: string;
  groupName: string;
  checkInTime: string;
  method: ScanMethod;
  status: AttendanceBadgeStatus;
  whatsAppStatus: WhatsAppDeliveryStatus;
}

export interface DailyAttendanceVolume {
  dayKey: 'sat' | 'sun' | 'mon' | 'tue' | 'wed' | 'thu' | 'fri';
  dayLabel: string;
  attendanceCount: number;
  isCurrentDay: boolean;
  isHoliday: boolean;
}

export interface DashboardOverviewResponse {
  renewalNotice: RenewalNoticeBanner;
  kpis: DashboardKpiSummary;
  upcomingSessions: TodaySessionItem[];
  recentAttendance: RecentAttendanceItem[];
  weeklyDistribution: DailyAttendanceVolume[];
}
