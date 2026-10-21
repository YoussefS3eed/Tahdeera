import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { PaymentsComponent } from './payments.component';
import { FinanceStore } from '../../../core/stores/finance.store';
import { MOCK_BILLING_ROSTER, MOCK_TRANSACTIONS, MOCK_FINANCIAL_KPIS } from '../../../core/mocks/f4-mock-data';

describe('PaymentsComponent', () => {
  let component: PaymentsComponent;
  let fixture: ComponentFixture<PaymentsComponent>;
  let financeStore: FinanceStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaymentsComponent, HttpClientTestingModule],
      providers: [FinanceStore]
    }).compileComponents();

    fixture = TestBed.createComponent(PaymentsComponent);
    component = fixture.componentInstance;
    financeStore = TestBed.inject(FinanceStore);

    // Mock store data
    financeStore.billingRoster.set([...MOCK_BILLING_ROSTER]);
    financeStore.transactionsLedger.set([...MOCK_TRANSACTIONS]);
    financeStore.kpis.set({ ...MOCK_FINANCIAL_KPIS });

    fixture.detectChanges();
  });

  it('should create the component and render KPIs', () => {
    expect(component).toBeTruthy();
    expect(component.filteredRoster().length).toBe(MOCK_BILLING_ROSTER.length);
    expect(component.filteredLedger().length).toBe(MOCK_TRANSACTIONS.length);
  });

  it('should filter roster by query (student name or code)', () => {
    component.searchQuery.set('سارة');
    expect(component.filteredRoster().length).toBe(1);
    expect(component.filteredRoster()[0].studentName).toContain('سارة');

    component.searchQuery.set(MOCK_BILLING_ROSTER[0].studentCode);
    expect(component.filteredRoster().length).toBe(1);
    expect(component.filteredRoster()[0].studentCode).toBe(MOCK_BILLING_ROSTER[0].studentCode);
  });

  it('should filter roster by paymentStatus', () => {
    component.statusFilter.set('Overdue');
    expect(component.filteredRoster().every(s => s.paymentStatus === 'Overdue')).toBeTrue();
  });

  it('should switch between roster and ledger tabs', () => {
    expect(component.activeTab()).toBe('roster');
    component.activeTab.set('ledger');
    expect(component.activeTab()).toBe('ledger');
  });

  it('should open record payment modal with selected student', () => {
    const student = MOCK_BILLING_ROSTER[0];
    component.openRecordPayment(student);

    expect(component.selectedStudent()).toEqual(student);
    expect(component.showRecordModal()).toBeTrue();
  });

  it('should open receipt modal with selected transaction', () => {
    const tx = MOCK_TRANSACTIONS[0];
    component.openReceipt(tx);

    expect(component.selectedTransaction()).toEqual(tx);
    expect(component.showReceiptModal()).toBeTrue();
  });

  it('should open reversal modal with target transaction', () => {
    const tx = MOCK_TRANSACTIONS[0];
    component.openReversal(tx);

    expect(component.reversalTarget()).toEqual(tx);
    expect(component.showReversalModal()).toBeTrue();
  });
});
