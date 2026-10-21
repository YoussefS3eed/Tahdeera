import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { WhatsAppStore } from './whatsapp.store';
import { MOCK_WHATSAPP_TEMPLATES, MOCK_OUTBOX_LOGS, MOCK_CONSENTS } from '../mocks/f4-mock-data';

describe('WhatsAppStore', () => {
  let store: WhatsAppStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [WhatsAppStore]
    });

    store = TestBed.inject(WhatsAppStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    store.ngOnDestroy();
    httpMock.verify();
  });

  it('should initialize with default signals', () => {
    expect(store.activeSubTab()).toBe('template');
    expect(store.activeTemplateType()).toBe('AttendanceCheckIn');
    expect(store.outboxLogs()).toEqual([]);
    expect(store.consents()).toEqual([]);
    expect(store.filterStatus()).toBe('all');
    expect(store.searchPhone()).toBe('');
  });

  it('should load templates and compute currentTemplate', async () => {
    const loadPromise = store.loadTemplates();
    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/whatsapp/templates');
    expect(req.request.method).toBe('GET');
    req.flush(MOCK_WHATSAPP_TEMPLATES);

    await loadPromise;
    expect(Object.keys(store.templates()).length).toBe(Object.keys(MOCK_WHATSAPP_TEMPLATES).length);
    expect(store.currentTemplate()?.type).toBe('AttendanceCheckIn');
  });

  it('should update a template', async () => {
    const newText = 'نص تجريبي جديد {StudentName}';
    const updatePromise = store.updateTemplate('AttendanceCheckIn', newText);

    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/whatsapp/templates/AttendanceCheckIn');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ rawText: newText });

    req.flush({
      id: 'wt-1',
      type: 'AttendanceCheckIn',
      nameAr: 'تأكيد الحضور',
      rawText: newText,
      availableTokens: ['StudentName'],
      updatedAt: '2026-10-21T10:00:00Z'
    });

    await updatePromise;
    expect(store.templates()['AttendanceCheckIn'].rawText).toBe(newText);
  });

  it('should load outbox logs and filter by status and query', async () => {
    const loadPromise = store.loadOutboxLogs();
    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/whatsapp/outbox');
    expect(req.request.method).toBe('GET');
    req.flush(MOCK_OUTBOX_LOGS);

    await loadPromise;
    expect(store.outboxLogs().length).toBe(MOCK_OUTBOX_LOGS.length);

    // Filter by status 'Pending'
    store.filterStatus.set('Pending');
    expect(store.filteredLogs().every(l => l.status === 'Pending')).toBeTrue();

    // Filter by query (phone or student name)
    store.filterStatus.set('all');
    store.searchPhone.set('عمر');
    expect(store.filteredLogs().every(l => l.studentName.includes('عمر'))).toBeTrue();
  });

  it('should cancel a pending message', async () => {
    store.outboxLogs.set([...MOCK_OUTBOX_LOGS]);
    const pendingLog = MOCK_OUTBOX_LOGS.find(l => l.status === 'Pending')!;

    const cancelPromise = store.cancelPendingMessage(pendingLog.id);
    const req = httpMock.expectOne(`/api/v1/workspaces/ws-main/whatsapp/outbox/${pendingLog.id}/cancel`);
    expect(req.request.method).toBe('POST');

    const cancelledLog = { ...pendingLog, status: 'Cancelled' as const, bufferSecondsRemaining: 0 };
    req.flush(cancelledLog);

    await cancelPromise;
    const updated = store.outboxLogs().find(l => l.id === pendingLog.id);
    expect(updated?.status).toBe('Cancelled');
  });

  it('should retry a failed message', async () => {
    store.outboxLogs.set([...MOCK_OUTBOX_LOGS]);
    const failedLog = MOCK_OUTBOX_LOGS.find(l => l.status === 'FailedPermanent')!;

    const retryPromise = store.retryFailedMessage(failedLog.id);
    const req = httpMock.expectOne(`/api/v1/workspaces/ws-main/whatsapp/outbox/${failedLog.id}/retry`);
    expect(req.request.method).toBe('POST');

    const retriedLog = { ...failedLog, status: 'Pending' as const, bufferSecondsRemaining: 30 };
    req.flush(retriedLog);

    await retryPromise;
    const updated = store.outboxLogs().find(l => l.id === failedLog.id);
    expect(updated?.status).toBe('Pending');
  });

  it('should load consents and toggle consent', async () => {
    const loadPromise = store.loadConsents();
    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/whatsapp/consents');
    expect(req.request.method).toBe('GET');
    req.flush(MOCK_CONSENTS);

    await loadPromise;
    expect(store.consents().length).toBe(MOCK_CONSENTS.length);

    const target = MOCK_CONSENTS[0];
    const togglePromise = store.toggleConsent(target.studentId, false);

    const putReq = httpMock.expectOne(`/api/v1/workspaces/ws-main/whatsapp/consents/${target.studentId}`);
    expect(putReq.request.method).toBe('PUT');
    expect(putReq.request.body).toEqual({ whatsappOptIn: false });

    putReq.flush({ ...target, whatsappOptIn: false });

    await togglePromise;
    const updated = store.consents().find(c => c.studentId === target.studentId);
    expect(updated?.whatsappOptIn).toBeFalse();
  });
});
