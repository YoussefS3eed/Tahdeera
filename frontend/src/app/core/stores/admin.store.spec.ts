import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AdminStore } from './admin.store';
import { MOCK_PLATFORM_OVERVIEW_KPIS, MOCK_RENEWAL_WATCHLIST, MOCK_ADMIN_WORKSPACES } from '../mocks/admin-mock-data';

describe('AdminStore', () => {
  let store: AdminStore;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdminStore,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    store = TestBed.inject(AdminStore);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should load overview KPIs and watchlist', () => {
    store.loadOverview().subscribe();

    const req = httpTesting.expectOne('/api/v1/admin/overview');
    expect(req.request.method).toBe('GET');
    req.flush({
      kpis: MOCK_PLATFORM_OVERVIEW_KPIS,
      renewalWatchlist: MOCK_RENEWAL_WATCHLIST
    });

    expect(store.overviewKpis()).toEqual(MOCK_PLATFORM_OVERVIEW_KPIS);
    expect(store.renewalWatchlist().length).toBe(MOCK_RENEWAL_WATCHLIST.length);
  });

  it('should load and filter workspaces', () => {
    store.loadWorkspaces('أوائل', 'Active').subscribe();

    const req = httpTesting.expectOne('/api/v1/admin/workspaces?query=%D8%A3%D9%88%D8%A7%D8%A6%D9%84&status=Active');
    expect(req.request.method).toBe('GET');
    req.flush(MOCK_ADMIN_WORKSPACES);

    expect(store.workspaces().length).toBe(MOCK_ADMIN_WORKSPACES.length);
    expect(store.totalWorkspacesCount()).toBe(MOCK_ADMIN_WORKSPACES.length);
  });

  it('should suspend a workspace with mandatory reason', () => {
    store.workspaces.set([...MOCK_ADMIN_WORKSPACES]);
    const targetId = MOCK_ADMIN_WORKSPACES[0].id;

    store.suspendWorkspace(targetId, { suspensionReason: 'تأخر في سداد الاشتراك الشهري' }).subscribe();

    const req = httpTesting.expectOne(`/api/v1/admin/workspaces/${targetId}/suspend`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true });

    const suspended = store.workspaces().find(w => w.id === targetId);
    expect(suspended?.status).toBe('Suspended');
    expect(suspended?.suspensionReason).toBe('تأخر في سداد الاشتراك الشهري');
  });
});
