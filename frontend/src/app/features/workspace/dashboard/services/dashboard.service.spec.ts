import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DashboardService } from './dashboard.service';
import { AuthStore } from '../../../../core/stores/auth.store';
import { DashboardOverviewResponse } from '../../../../core/models/dashboard.models';

describe('DashboardService', () => {
  let service: DashboardService;
  let httpTestingController: HttpTestingController;
  let authStore: AuthStore;

  const mockResponse: DashboardOverviewResponse = {
    renewalNotice: {
      showBanner: false,
      daysRemaining: 45,
      expiryDate: '2026-12-05T00:00:00Z',
      supportHotlines: ['01272785618', '01019482687']
    },
    kpis: {
      totalStudents: { totalCount: 248, activeCount: 230, inactiveCount: 18 },
      presentToday: { presentCount: 142, attendancePercentage: 88, totalScheduledRecords: 160 },
      absentToday: { absentCount: 18, unexcusedCount: 14, excusedCount: 4 },
      unpaidTuition: { totalOverdueAmount: 12450, currency: 'EGP', unpaidStudentCount: 30 }
    },
    upcomingSessions: [],
    recentAttendance: [],
    weeklyDistribution: []
  };

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        DashboardService,
        AuthStore,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(DashboardService);
    httpTestingController = TestBed.inject(HttpTestingController);
    authStore = TestBed.inject(AuthStore);

    authStore.activeWorkspaceId.set('ws_test_99');
  });

  afterEach(() => {
    httpTestingController.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch dashboard overview for the active workspace', () => {
    service.getOverview().subscribe(res => {
      expect(res).toEqual(mockResponse);
      expect(res.kpis.totalStudents.totalCount).toBe(248);
    });

    const req = httpTestingController.expectOne('/api/v1/workspaces/ws_test_99/dashboard');
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });
});
