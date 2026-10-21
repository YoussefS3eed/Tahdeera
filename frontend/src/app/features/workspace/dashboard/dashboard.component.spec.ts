import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardComponent } from './dashboard.component';
import { DashboardService } from './services/dashboard.service';
import { AuthStore } from '../../../core/stores/auth.store';
import { LocalizationService } from '../../../core/services/localization.service';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { By } from '@angular/platform-browser';
import { DashboardOverviewResponse } from '../../../core/models/dashboard.models';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let dashboardServiceSpy: jasmine.SpyObj<DashboardService>;
  let authStore: AuthStore;

  const mockOverviewWithTuition: DashboardOverviewResponse = {
    renewalNotice: {
      showBanner: true,
      daysRemaining: 5,
      expiryDate: '2026-10-25T00:00:00Z',
      supportHotlines: ['01272785618', '01019482687']
    },
    kpis: {
      totalStudents: { totalCount: 248, activeCount: 240, inactiveCount: 8 },
      presentToday: { presentCount: 142, attendancePercentage: 88, totalScheduledRecords: 160 },
      absentToday: { absentCount: 18, unexcusedCount: 14, excusedCount: 4 },
      unpaidTuition: { totalOverdueAmount: 12450, currency: 'EGP', unpaidStudentCount: 30 }
    },
    upcomingSessions: [
      {
        id: 'sess_1',
        groupName: 'رياضيات أ',
        subject: 'رياضيات',
        gradeLevel: '3 ثانوي',
        scheduledStart: '14:00',
        scheduledEnd: '15:30',
        status: 'Completed',
        statusBadgeLabel: 'مكتملة',
        presentCount: 25,
        rosterCapacity: 30,
        actionRoute: '/workspace/attendance',
        actionLabel: 'السجل'
      }
    ],
    recentAttendance: [
      {
        id: 'rec_1',
        studentId: 'st_1',
        studentCode: 'ST-1042',
        studentName: 'أحمد محمود',
        groupName: 'رياضيات أ',
        checkInTime: '14:02',
        method: 'NFC',
        status: 'Present',
        whatsAppStatus: 'Delivered'
      }
    ],
    weeklyDistribution: [
      { dayKey: 'sat', dayLabel: 'السبت', attendanceCount: 72, isCurrentDay: false, isHoliday: false },
      { dayKey: 'thu', dayLabel: 'الخميس', attendanceCount: 76, isCurrentDay: true, isHoliday: false },
      { dayKey: 'fri', dayLabel: 'الجمعة', attendanceCount: 0, isCurrentDay: false, isHoliday: true }
    ]
  };

  const mockOverviewWithoutTuition: DashboardOverviewResponse = {
    renewalNotice: {
      showBanner: false,
      daysRemaining: 45,
      expiryDate: '2026-12-05T00:00:00Z',
      supportHotlines: ['01272785618', '01019482687']
    },
    kpis: {
      totalStudents: { totalCount: 248, activeCount: 240, inactiveCount: 8 },
      presentToday: { presentCount: 142, attendancePercentage: 88, totalScheduledRecords: 160 },
      absentToday: { absentCount: 18, unexcusedCount: 14, excusedCount: 4 },
      unpaidTuition: null
    },
    upcomingSessions: [],
    recentAttendance: [],
    weeklyDistribution: []
  };

  beforeEach(async () => {
    localStorage.clear();
    dashboardServiceSpy = jasmine.createSpyObj('DashboardService', ['getOverview']);

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        AuthStore,
        { provide: DashboardService, useValue: dashboardServiceSpy },
        {
          provide: LocalizationService,
          useValue: {
            currentLang: () => 'ar',
            isRtl: () => true,
            translate: (key: string) => key === 'DASHBOARD.UNAUTHORIZED_ROLE_ACCESS' ? 'عذرًا، هذا القسم مخصص لمالك الحساب والإدارة فقط.' : ''
          }
        },
        provideRouter([])
      ]
    }).compileComponents();

    authStore = TestBed.inject(AuthStore);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should render skeleton loaders during initial load', () => {
    // Return observable that has not emitted yet
    dashboardServiceSpy.getOverview.and.returnValue(of(mockOverviewWithTuition));
    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;

    // Before detectChanges loads data, check skeleton items
    expect(component.isLoading()).toBeTrue();
  });

  it('should render 4 KPI cards including Unpaid Tuition for Owner role', () => {
    authStore.workspaces.set([
      {
        id: 'ws_01',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Active',
        role: 'Owner',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 5,
        subscriptionExpiresAt: '2026-10-25T00:00:00Z'
      }
    ]);
    authStore.activeWorkspaceId.set('ws_01');

    dashboardServiceSpy.getOverview.and.returnValue(of(mockOverviewWithTuition));

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.isLoading()).toBeFalse();
    expect(component.canViewFinancials()).toBeTrue();

    const tuitionCard = fixture.debugElement.query(By.css('.tuition-card'));
    expect(tuitionCard).toBeTruthy();
    expect(tuitionCard.nativeElement.textContent).toContain('12450');
  });

  it('should STRICTLY EXCLUDE Unpaid Tuition card from DOM for Teacher role (Principle 9)', () => {
    authStore.workspaces.set([
      {
        id: 'ws_01',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Active',
        role: 'Teacher',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 45,
        subscriptionExpiresAt: '2026-12-05T00:00:00Z'
      }
    ]);
    authStore.activeWorkspaceId.set('ws_01');

    dashboardServiceSpy.getOverview.and.returnValue(of(mockOverviewWithoutTuition));

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.canViewFinancials()).toBeFalse();

    const tuitionCard = fixture.debugElement.query(By.css('.tuition-card'));
    expect(tuitionCard).toBeNull();
  });

  it('should STRICTLY EXCLUDE Unpaid Tuition card from DOM for Assistant role (CONF-09)', () => {
    authStore.workspaces.set([
      {
        id: 'ws_01',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Active',
        role: 'Assistant',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 45,
        subscriptionExpiresAt: '2026-12-05T00:00:00Z'
      }
    ]);
    authStore.activeWorkspaceId.set('ws_01');

    dashboardServiceSpy.getOverview.and.returnValue(of(mockOverviewWithoutTuition));

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.canViewFinancials()).toBeFalse();

    const tuitionCard = fixture.debugElement.query(By.css('.tuition-card'));
    expect(tuitionCard).toBeNull();
  });

  it('should STRICTLY EXCLUDE Unpaid Tuition card from DOM for Receptionist role (CONF-09)', () => {
    authStore.workspaces.set([
      {
        id: 'ws_01',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Active',
        role: 'Receptionist',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 45,
        subscriptionExpiresAt: '2026-12-05T00:00:00Z'
      }
    ]);
    authStore.activeWorkspaceId.set('ws_01');

    dashboardServiceSpy.getOverview.and.returnValue(of(mockOverviewWithoutTuition));

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.canViewFinancials()).toBeFalse();

    const tuitionCard = fixture.debugElement.query(By.css('.tuition-card'));
    expect(tuitionCard).toBeNull();
  });

  it('should display renewal banner when showBanner is true', () => {
    authStore.workspaces.set([
      {
        id: 'ws_01',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Active',
        role: 'Teacher',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 5,
        subscriptionExpiresAt: '2026-10-25T00:00:00Z'
      }
    ]);
    authStore.activeWorkspaceId.set('ws_01');
    dashboardServiceSpy.getOverview.and.returnValue(of(mockOverviewWithTuition));

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    const banner = fixture.debugElement.query(By.css('.renewal-banner'));
    expect(banner).toBeTruthy();
    expect(banner.nativeElement.textContent).toContain('01272785618');
  });

  it('should trigger getOverview when manual refresh button is clicked', () => {
    authStore.workspaces.set([
      {
        id: 'ws_01',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Active',
        role: 'Teacher',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 45,
        subscriptionExpiresAt: '2026-12-05T00:00:00Z'
      }
    ]);
    authStore.activeWorkspaceId.set('ws_01');
    dashboardServiceSpy.getOverview.and.returnValue(of(mockOverviewWithTuition));

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(dashboardServiceSpy.getOverview).toHaveBeenCalledTimes(1);

    const refreshBtn = fixture.debugElement.query(By.css('.refresh-btn'));
    expect(refreshBtn).toBeTruthy();
    refreshBtn.nativeElement.click();

    expect(dashboardServiceSpy.getOverview).toHaveBeenCalledTimes(2);
  });

  it('should render unauthorized role alert banner when history.state contains accessDenied: true', () => {
    history.replaceState({ accessDenied: true, messageKey: 'DASHBOARD.UNAUTHORIZED_ROLE_ACCESS' }, '');
    dashboardServiceSpy.getOverview.and.returnValue(of(mockOverviewWithTuition));

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.unauthorizedAlertVisible()).toBeTrue();
    const banner = fixture.debugElement.query(By.css('.unauthorized-role-banner'));
    expect(banner).toBeTruthy();
    expect(banner.nativeElement.textContent).toContain('عذرًا، هذا القسم مخصص لمالك الحساب والإدارة فقط.');

    expect(history.state.accessDenied).toBeUndefined();
  });

  it('should dismiss unauthorized role alert banner when dismiss button is clicked', () => {
    history.replaceState({ accessDenied: true, messageKey: 'DASHBOARD.UNAUTHORIZED_ROLE_ACCESS' }, '');
    dashboardServiceSpy.getOverview.and.returnValue(of(mockOverviewWithTuition));

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.unauthorizedAlertVisible()).toBeTrue();
    const dismissBtn = fixture.debugElement.query(By.css('.dismiss-role-alert-btn'));
    expect(dismissBtn).toBeTruthy();
    dismissBtn.nativeElement.click();
    fixture.detectChanges();

    expect(component.unauthorizedAlertVisible()).toBeFalse();
    expect(fixture.debugElement.query(By.css('.unauthorized-role-banner'))).toBeNull();
  });

  it('should not render unauthorized alert banner when navigation state has no accessDenied', () => {
    history.replaceState({}, '');
    dashboardServiceSpy.getOverview.and.returnValue(of(mockOverviewWithTuition));

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.unauthorizedAlertVisible()).toBeFalse();
    expect(fixture.debugElement.query(By.css('.unauthorized-role-banner'))).toBeNull();
  });
});
