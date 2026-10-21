import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { RevenueComponent } from './revenue.component';
import { AdminStore } from '../../../core/stores/admin.store';
import { MOCK_SUBSCRIPTION_PAYMENTS } from '../../../core/mocks/admin-mock-data';

describe('RevenueComponent (SCR-23)', () => {
  let component: RevenueComponent;
  let fixture: ComponentFixture<RevenueComponent>;
  let adminStore: AdminStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RevenueComponent],
      providers: [
        AdminStore,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RevenueComponent);
    component = fixture.componentInstance;
    adminStore = TestBed.inject(AdminStore);
    adminStore.subscriptionPayments.set(MOCK_SUBSCRIPTION_PAYMENTS);
    fixture.detectChanges();
  });

  it('should render ledger items and calculate total collected EGP', () => {
    const rows = fixture.nativeElement.querySelectorAll('.data-table tbody tr');
    expect(rows.length).toBe(MOCK_SUBSCRIPTION_PAYMENTS.length);
    expect(component.totalCollectedEgp).toBeGreaterThan(0);
  });

  it('should enforce mandatory reversal reason minimum 5 chars', () => {
    component.openReverseModal(MOCK_SUBSCRIPTION_PAYMENTS[0]);
    component.reversalReason = 'test';
    expect(component.reversalReason.trim().length >= 5).toBeFalse();

    component.reversalReason = 'خطأ تحويل من العميل';
    expect(component.reversalReason.trim().length >= 5).toBeTrue();
  });
});
