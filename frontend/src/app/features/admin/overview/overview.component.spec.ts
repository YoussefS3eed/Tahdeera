import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminOverviewComponent } from './overview.component';
import { AdminStore } from '../../../core/stores/admin.store';
import { LocalizationService } from '../../../core/services/localization.service';
import { SupportedLanguage } from '../../../core/enums/domain.enums';
import { MOCK_PLATFORM_OVERVIEW_KPIS, MOCK_RENEWAL_WATCHLIST } from '../../../core/mocks/admin-mock-data';

describe('AdminOverviewComponent (SCR-19)', () => {
  let component: AdminOverviewComponent;
  let fixture: ComponentFixture<AdminOverviewComponent>;
  let adminStore: AdminStore;
  let localizationService: LocalizationService;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [AdminOverviewComponent],
      providers: [
        AdminStore,
        LocalizationService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    localizationService = TestBed.inject(LocalizationService);
    localizationService.setLanguage(SupportedLanguage.Arabic);

    fixture = TestBed.createComponent(AdminOverviewComponent);
    component = fixture.componentInstance;
    adminStore = TestBed.inject(AdminStore);
    adminStore.overviewKpis.set(MOCK_PLATFORM_OVERVIEW_KPIS);
    adminStore.renewalWatchlist.set(MOCK_RENEWAL_WATCHLIST);
    fixture.detectChanges();
  });

  it('should render 4 aggregate KPI cards', () => {
    const kpiCards = fixture.nativeElement.querySelectorAll('.kpi-card');
    expect(kpiCards.length).toBe(4);
    expect(fixture.nativeElement.textContent).toContain('إجمالي السناتر والمدرسين');
    expect(fixture.nativeElement.textContent).toContain('إجمالي الطلاب المسجلين');
    expect(fixture.nativeElement.textContent).toContain('الإيراد الشهري للمنصة');
  });

  it('should display the 7-day renewal watchlist', () => {
    const rows = fixture.nativeElement.querySelectorAll('.data-table tbody tr');
    expect(rows.length).toBe(MOCK_RENEWAL_WATCHLIST.length);
    expect(fixture.nativeElement.textContent).toContain(MOCK_RENEWAL_WATCHLIST[0].workspaceName);
  });
});
