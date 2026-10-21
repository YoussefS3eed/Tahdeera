import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SettingsStore } from './settings.store';
import { MOCK_WORKSPACE_SETTINGS, MOCK_SUBSCRIPTION_VIEW } from '../mocks/f4-mock-data';

describe('SettingsStore', () => {
  let store: SettingsStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SettingsStore]
    });

    store = TestBed.inject(SettingsStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should initialize with default null values', () => {
    expect(store.settings()).toBeNull();
    expect(store.subscription()).toBeNull();
    expect(store.isLoading()).toBeFalse();
    expect(store.isSaving()).toBeFalse();
  });

  it('should load settings and subscription', async () => {
    const sPromise = store.loadSettings();
    const sReq = httpMock.expectOne('/api/v1/workspaces/ws-main/settings');
    expect(sReq.request.method).toBe('GET');
    sReq.flush(MOCK_WORKSPACE_SETTINGS);
    await sPromise;

    expect(store.settings()?.profile?.centerNameAr).toBe('سنتر الأوائل التعليمي');

    const subPromise = store.loadSubscription();
    const subReq = httpMock.expectOne('/api/v1/workspaces/ws-main/subscription');
    expect(subReq.request.method).toBe('GET');
    subReq.flush(MOCK_SUBSCRIPTION_VIEW);
    await subPromise;

    expect(store.subscription()?.packageNameAr).toBe('الباقة الاحترافية');
    expect(store.subscription()?.activeStudentsCount).toBe(248);
  });

  it('should update profile', async () => {
    store.settings.set({ ...MOCK_WORKSPACE_SETTINGS });
    const updatePromise = store.updateProfile({ centerPhone: '01000000000' });

    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/settings/profile');
    expect(req.request.method).toBe('PUT');
    req.flush({ ...MOCK_WORKSPACE_SETTINGS.profile, centerPhone: '01000000000' });

    await updatePromise;
    expect(store.settings()?.profile?.centerPhone).toBe('01000000000');
  });

  it('should update attendance policies', async () => {
    store.settings.set({ ...MOCK_WORKSPACE_SETTINGS });
    const updatePromise = store.updateAttendancePolicies({ lateThresholdMinutes: 20 });

    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/settings/attendance-policies');
    expect(req.request.method).toBe('PUT');
    req.flush({ ...MOCK_WORKSPACE_SETTINGS.attendancePolicies, lateThresholdMinutes: 20 });

    await updatePromise;
    expect(store.settings()?.attendancePolicies?.lateThresholdMinutes).toBe(20);
  });

  it('should trigger backup download and update timestamp', async () => {
    const backupPromise = store.triggerBackupDownload();
    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/settings/backup');
    expect(req.request.method).toBe('POST');
    const mockBlob = new Blob(['mock-data'], { type: 'application/json' });
    req.flush(mockBlob);

    const blob = await backupPromise;
    expect(blob).toBeTruthy();
    expect(store.lastBackupTimestamp()).not.toBeNull();
  });
});
