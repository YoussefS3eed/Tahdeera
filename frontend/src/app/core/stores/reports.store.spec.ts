import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ReportsStore } from './reports.store';
import { MOCK_REPORTS_CATALOG, MOCK_REPORT_RESULTS } from '../mocks/f4-mock-data';

describe('ReportsStore', () => {
  let store: ReportsStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ReportsStore]
    });

    store = TestBed.inject(ReportsStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should initialize with default values', () => {
    expect(store.catalog()).toEqual([]);
    expect(store.activeReportId()).toBe('payments-ledger');
    expect(store.reportResult()).toBeNull();
    expect(store.isLoading()).toBeFalse();
  });

  it('should load report catalog', async () => {
    const loadPromise = store.loadCatalog();
    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/reports/catalog');
    expect(req.request.method).toBe('GET');
    req.flush(MOCK_REPORTS_CATALOG);

    await loadPromise;
    expect(store.catalog().length).toBe(MOCK_REPORTS_CATALOG.length);
    expect(store.activeReportMeta()?.id).toBe('payments-ledger');
  });

  it('should execute report and update reportResult', async () => {
    const runPromise = store.runReport({ reportId: 'payments-ledger' });
    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/reports/execute');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ reportId: 'payments-ledger' });
    req.flush(MOCK_REPORT_RESULTS['payments-ledger']);

    const res = await runPromise;
    expect(res.reportId).toBe('payments-ledger');
    expect(store.reportResult()).toEqual(MOCK_REPORT_RESULTS['payments-ledger']);
  });

  it('should select a report and trigger runReport', () => {
    store.catalog.set(MOCK_REPORTS_CATALOG);
    store.selectReport('daily-attendance');

    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/reports/execute');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ reportId: 'daily-attendance' });
    req.flush(MOCK_REPORT_RESULTS['daily-attendance']);

    expect(store.activeReportId()).toBe('daily-attendance');
    expect(store.activeReportMeta()?.titleAr).toBe('سجل الحضور اليومي للمجموعات');
  });
});
