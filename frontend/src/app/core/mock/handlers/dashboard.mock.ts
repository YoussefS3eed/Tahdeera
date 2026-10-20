import { HttpResponse, HttpErrorResponse, HttpRequest } from '@angular/common/http';
import { DashboardOverviewResponse } from '../../models/dashboard.models';

export function handleDashboardMockRequest(req: HttpRequest<unknown>): HttpResponse<unknown> | HttpErrorResponse | null {
  const url = req.url;
  const method = req.method;

  // GET /api/v1/workspaces/:workspaceId/dashboard
  if (method === 'GET' && url.includes('/dashboard')) {
    // Check Authorization header for role
    const authHeader = req.headers.get('Authorization') || '';
    const isAssistant = authHeader.includes('assistant');
    const isReceptionist = authHeader.includes('receptionist');
    const isNonFinancialRole = isAssistant || isReceptionist;

    const mockOverview: DashboardOverviewResponse = {
      renewalNotice: {
        showBanner: false,
        daysRemaining: 45,
        expiryDate: '2026-12-05',
        supportHotlines: ['01272785618', '01019482687']
      },
      kpis: {
        totalStudents: {
          totalCount: 248,
          activeCount: 240,
          inactiveCount: 8
        },
        presentToday: {
          presentCount: 142,
          attendancePercentage: 88,
          totalScheduledRecords: 160
        },
        absentToday: {
          absentCount: 18,
          unexcusedCount: 14,
          excusedCount: 4
        },
        // STRICT CONF-09 INVARIANT: Completely omitted for Assistant / Receptionist
        unpaidTuition: isNonFinancialRole
          ? null
          : {
              totalOverdueAmount: 12450,
              currency: 'ج.م',
              unpaidStudentCount: 34
            }
      },
      upcomingSessions: [
        {
          id: 'ses_01',
          groupName: 'رياضيات ب (3 ثانوي)',
          subject: 'رياضيات',
          gradeLevel: 'الصف الثالث الثانوي',
          scheduledStart: '14:00',
          scheduledEnd: '15:30',
          status: 'Completed',
          statusBadgeLabel: 'مكتملة (25 حاضر)',
          presentCount: 25,
          rosterCapacity: 28,
          actionRoute: '/workspace/attendance',
          actionLabel: 'السجل'
        },
        {
          id: 'ses_02',
          groupName: 'رياضيات أ (3 ثانوي)',
          subject: 'رياضيات',
          gradeLevel: 'الصف الثالث الثانوي',
          scheduledStart: '16:00',
          scheduledEnd: '17:30',
          status: 'InProgress',
          statusBadgeLabel: 'بانتظار الإغلاق',
          presentCount: 22,
          rosterCapacity: 25,
          actionRoute: '/workspace/nfc-station',
          actionLabel: 'تحضير NFC'
        },
        {
          id: 'ses_03',
          groupName: 'فيزياء أ (3 ثانوي)',
          subject: 'فيزياء',
          gradeLevel: 'الصف الثالث الثانوي',
          scheduledStart: '18:00',
          scheduledEnd: '19:30',
          status: 'Waiting',
          statusBadgeLabel: 'مجدولة',
          presentCount: 0,
          rosterCapacity: 30,
          actionRoute: '/workspace/attendance',
          actionLabel: 'التفاصيل'
        }
      ],
      recentAttendance: [
        {
          id: 'att_01',
          studentId: 'std_101',
          studentCode: 'ST-1042',
          studentName: 'أحمد محمود سالم',
          groupName: 'رياضيات أ (3 ثانوي)',
          checkInTime: '16:02',
          method: 'NFC',
          status: 'Present',
          whatsAppStatus: 'Delivered'
        },
        {
          id: 'att_02',
          studentId: 'std_102',
          studentCode: 'ST-1088',
          studentName: 'مريم سامح توفيق',
          groupName: 'رياضيات أ (3 ثانوي)',
          checkInTime: '16:11',
          method: 'Manual',
          status: 'Late',
          whatsAppStatus: 'Pending'
        },
        {
          id: 'att_03',
          studentId: 'std_103',
          studentCode: 'ST-1015',
          studentName: 'يوسف إبراهيم خليل',
          groupName: 'رياضيات أ (3 ثانوي)',
          checkInTime: '16:15',
          method: 'NFC',
          status: 'Present',
          whatsAppStatus: 'Delivered'
        }
      ],
      weeklyDistribution: [
        { dayKey: 'sat', dayLabel: 'السبت', attendanceCount: 115, isCurrentDay: false, isHoliday: false },
        { dayKey: 'sun', dayLabel: 'الأحد', attendanceCount: 142, isCurrentDay: true, isHoliday: false },
        { dayKey: 'mon', dayLabel: 'الاثنين', attendanceCount: 98, isCurrentDay: false, isHoliday: false },
        { dayKey: 'tue', dayLabel: 'الثلاثاء', attendanceCount: 130, isCurrentDay: false, isHoliday: false },
        { dayKey: 'wed', dayLabel: 'الأربعاء', attendanceCount: 85, isCurrentDay: false, isHoliday: false },
        { dayKey: 'thu', dayLabel: 'الخميس', attendanceCount: 140, isCurrentDay: false, isHoliday: false },
        { dayKey: 'fri', dayLabel: 'الجمعة', attendanceCount: 0, isCurrentDay: false, isHoliday: true }
      ]
    };

    return new HttpResponse({ status: 200, body: mockOverview });
  }

  return null;
}
