import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ReportsComponent } from './reports.component';
import { ReportsStore } from '../../../core/stores/reports.store';
import { MOCK_REPORTS_CATALOG, MOCK_REPORT_RESULTS } from '../../../core/mocks/f4-mock-data';

describe('ReportsComponent', () => {
  let component: ReportsComponent;
  let fixture: ComponentFixture<ReportsComponent>;
  let store: ReportsStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReportsComponent, HttpClientTestingModule],
      providers: [ReportsStore]
    }).compileComponents();

    fixture = TestBed.createComponent(ReportsComponent);
    component = fixture.componentInstance;
    store = TestBed.inject(ReportsStore);

    // Mock store data
    store.catalog.set([...MOCK_REPORTS_CATALOG]);
    store.activeReportId.set('payments-ledger');
    store.reportResult.set(MOCK_REPORT_RESULTS['payments-ledger']);

    fixture.detectChanges();
  });

  it('should create reports component', () => {
    expect(component).toBeTruthy();
    expect(store.catalog().length).toBe(MOCK_REPORTS_CATALOG.length);
    expect(store.reportResult()?.reportId).toBe('payments-ledger');
  });
});
