import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { FinanceStore } from './finance.store';
import { MOCK_FINANCIAL_KPIS, MOCK_BILLING_ROSTER, MOCK_TRANSACTIONS } from '../mocks/f4-mock-data';

describe('FinanceStore', () => {
  let store: FinanceStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [FinanceStore]
    });

    store = TestBed.inject(FinanceStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should initialize with default empty signals', () => {
    expect(store.billingRoster()).toEqual([]);
    expect(store.transactionsLedger()).toEqual([]);
    expect(store.kpis()).toBeNull();
    expect(store.isLoading()).toBeFalse();
  });

  it('should load billing roster and ledger transactions', async () => {
    const loadPromise = store.loadBillingData();

    const summaryReq = httpMock.expectOne('/api/v1/workspaces/ws-main/payments/summary');
    expect(summaryReq.request.method).toBe('GET');
    summaryReq.flush({ kpis: MOCK_FINANCIAL_KPIS, students: MOCK_BILLING_ROSTER });

    const ledgerReq = httpMock.expectOne('/api/v1/workspaces/ws-main/payments/ledger');
    expect(ledgerReq.request.method).toBe('GET');
    ledgerReq.flush({ transactions: MOCK_TRANSACTIONS });

    await loadPromise;

    expect(store.billingRoster().length).toBeGreaterThan(0);
    expect(store.kpis()?.totalRequiredEgp).toBe(112500.0);
    expect(store.transactionsLedger().length).toBeGreaterThan(0);
  });

  it('should record payment and update student balance', async () => {
    store.billingRoster.set([
      {
        studentId: 'st-test',
        studentName: 'طالب تجريبي',
        studentCode: 'ST-9999',
        gradeName: 'الثالث الثانوي',
        groupName: 'أ',
        requiredFee: 450,
        paidAmount: 0,
        remainingFee: 450,
        paymentStatus: 'Unpaid'
      }
    ]);
    store.kpis.set({ ...MOCK_FINANCIAL_KPIS });

    const recordPromise = store.recordPayment({
      studentId: 'st-test',
      amount: 450,
      paymentMethod: 'Cash'
    });

    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/payments');
    expect(req.request.method).toBe('POST');
    req.flush({
      id: 'REC-NEW-01',
      workspaceId: 'ws-main',
      studentId: 'st-test',
      studentName: 'طالب تجريبي',
      studentCode: 'ST-9999',
      amount: 450,
      paymentMethod: 'Cash',
      recordedAtUtc: new Date().toISOString(),
      recordedByUserId: 'usr-1',
      recordedByUserName: 'المحاسب',
      status: 'Completed',
      allocations: []
    });

    const tx = await recordPromise;

    expect(tx.id).toBe('REC-NEW-01');
    expect(store.activeReceipt()?.id).toBe('REC-NEW-01');
    const updatedStudent = store.billingRoster().find(s => s.studentId === 'st-test');
    expect(updatedStudent?.remainingFee).toBe(0);
    expect(updatedStudent?.paymentStatus).toBe('Paid');
  });

  it('should reverse payment and restore student remaining fee', async () => {
    store.billingRoster.set([
      {
        studentId: 'st-test',
        studentName: 'طالب تجريبي',
        studentCode: 'ST-9999',
        gradeName: 'الثالث الثانوي',
        groupName: 'أ',
        requiredFee: 450,
        paidAmount: 450,
        remainingFee: 0,
        paymentStatus: 'Paid'
      }
    ]);
    store.transactionsLedger.set([
      {
        id: 'REC-REV-01',
        workspaceId: 'ws-main',
        studentId: 'st-test',
        studentName: 'طالب تجريبي',
        studentCode: 'ST-9999',
        amount: 450,
        paymentMethod: 'Cash',
        recordedAtUtc: new Date().toISOString(),
        recordedByUserId: 'usr-1',
        recordedByUserName: 'المحاسب',
        status: 'Completed',
        allocations: []
      }
    ]);
    store.kpis.set({ ...MOCK_FINANCIAL_KPIS });

    const reversePromise = store.reversePayment('REC-REV-01', 'خطأ في استلام المبلغ نقداً');

    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/payments/REC-REV-01/reverse');
    expect(req.request.method).toBe('POST');
    req.flush({
      id: 'REC-REV-01',
      workspaceId: 'ws-main',
      studentId: 'st-test',
      studentName: 'طالب تجريبي',
      studentCode: 'ST-9999',
      amount: 450,
      paymentMethod: 'Cash',
      recordedAtUtc: new Date().toISOString(),
      recordedByUserId: 'usr-1',
      recordedByUserName: 'المحاسب',
      status: 'Reversed',
      reversalReason: 'خطأ في استلام المبلغ نقداً',
      allocations: []
    });

    const tx = await reversePromise;
    expect(tx.status).toBe('Reversed');
    const updatedStudent = store.billingRoster().find(s => s.studentId === 'st-test');
    expect(updatedStudent?.remainingFee).toBe(450);
    expect(updatedStudent?.paymentStatus).toBe('Unpaid');
  });
});
