import { TestBed } from '@angular/core/testing';
import { AttendanceSyncService } from './attendance-sync.service';
import { OfflineStorageService } from './offline-storage.service';
import { AcademicService } from './academic.service';
import { of } from 'rxjs';
import { SyncBatchResponse } from '../models/academic.models';

describe('AttendanceSyncService (DEC-04 Offline Sync & Conflict Engine)', () => {
  let service: AttendanceSyncService;
  let offlineStorageMock: jasmine.SpyObj<OfflineStorageService>;
  let academicServiceMock: jasmine.SpyObj<AcademicService>;

  beforeEach(() => {
    offlineStorageMock = jasmine.createSpyObj('OfflineStorageService', [
      'getSyncQueue',
      'updateSyncStatus',
      'refreshPendingCount'
    ]);
    academicServiceMock = jasmine.createSpyObj('AcademicService', ['syncBatch']);

    offlineStorageMock.getSyncQueue.and.returnValue(Promise.resolve([]));

    TestBed.configureTestingModule({
      providers: [
        AttendanceSyncService,
        { provide: OfflineStorageService, useValue: offlineStorageMock },
        { provide: AcademicService, useValue: academicServiceMock }
      ]
    });

    service = TestBed.inject(AttendanceSyncService);
  });

  it('should initialize and compute pending/conflict counts', async () => {
    offlineStorageMock.getSyncQueue.and.returnValue(Promise.resolve([
      {
        localId: 1,
        operationId: 'op-1',
        workspaceId: 'ws-1',
        sessionId: 'sess-1',
        studentId: 'std-1',
        identifierUsed: '04:A2:7C:19',
        source: 'NfcCard',
        scannedAtUtc: '2026-10-21T08:00:00Z',
        syncStatus: 'Pending',
        retryCount: 0
      },
      {
        localId: 2,
        operationId: 'op-2',
        workspaceId: 'ws-1',
        sessionId: 'sess-2',
        studentId: 'std-2',
        identifierUsed: '04:B3:8D:2A',
        source: 'NfcCard',
        scannedAtUtc: '2026-10-21T08:05:00Z',
        syncStatus: 'Conflict',
        conflictReason: 'SessionCancelledOnline',
        retryCount: 0
      }
    ]));

    await service.refreshCounts();

    expect(service.pendingCount()).toBe(1);
    expect(service.conflictCount()).toBe(1);
  });

  it('should dispatch pending items in batch and mark clean records as Synced', async () => {
    offlineStorageMock.getSyncQueue.and.returnValue(Promise.resolve([
      {
        localId: 10,
        operationId: 'op-10',
        workspaceId: 'ws-cairo',
        sessionId: 'sess-1',
        studentId: 'std-10',
        identifierUsed: '04:11:22:33',
        source: 'NfcCard',
        scannedAtUtc: '2026-10-21T08:10:00Z',
        syncStatus: 'Pending',
        retryCount: 0
      }
    ]));

    const mockResponse: SyncBatchResponse = {
      batchId: 'batch-1',
      processedAtUtc: '2026-10-21T08:11:00Z',
      syncedCount: 1,
      conflictCount: 0,
      results: [
        {
          operationId: 'op-10',
          status: 'Synced'
        }
      ]
    };
    academicServiceMock.syncBatch.and.returnValue(of(mockResponse));

    const result = await service.syncPending('ws-cairo');

    expect(academicServiceMock.syncBatch).toHaveBeenCalled();
    expect(offlineStorageMock.updateSyncStatus).toHaveBeenCalledWith(10, 'Synced');
    expect(result.synced).toBe(1);
  });

  it('should detect server SessionCancelledOnline conflict and update status to Conflict (DEC-04)', async () => {
    offlineStorageMock.getSyncQueue.and.returnValue(Promise.resolve([
      {
        localId: 11,
        operationId: 'op-11',
        workspaceId: 'ws-cairo',
        sessionId: 'sess-cancelled',
        studentId: 'std-11',
        identifierUsed: '04:44:55:66',
        source: 'NfcCard',
        scannedAtUtc: '2026-10-21T08:15:00Z',
        syncStatus: 'Pending',
        retryCount: 0
      }
    ]));

    const mockResponse: SyncBatchResponse = {
      batchId: 'batch-2',
      processedAtUtc: '2026-10-21T08:16:00Z',
      syncedCount: 0,
      conflictCount: 1,
      results: [
        {
          operationId: 'op-11',
          status: 'Conflict',
          conflictReason: 'SessionCancelledOnline: عطل بالتكييف'
        }
      ]
    };
    academicServiceMock.syncBatch.and.returnValue(of(mockResponse));

    const result = await service.syncPending('ws-cairo');

    expect(offlineStorageMock.updateSyncStatus).toHaveBeenCalledWith(
      11,
      'Conflict',
      'SessionCancelledOnline: عطل بالتكييف'
    );
    expect(result.conflicts).toBe(1);
  });

  it('should resolve conflict by approving exceptional attendance', async () => {
    offlineStorageMock.getSyncQueue.and.returnValue(Promise.resolve([
      {
        localId: 12,
        operationId: 'op-12',
        workspaceId: 'ws-cairo',
        sessionId: 'sess-cancelled',
        studentId: 'std-12',
        identifierUsed: '04:77:88:99',
        source: 'NfcCard',
        scannedAtUtc: '2026-10-21T08:20:00Z',
        syncStatus: 'Conflict',
        conflictReason: 'SessionCancelledOnline',
        retryCount: 0
      }
    ]));

    await service.resolveConflict('op-12', 'ApproveExceptional');

    expect(offlineStorageMock.updateSyncStatus).toHaveBeenCalledWith(12, 'Synced');
  });

  it('should resolve conflict by voiding record with justification reason', async () => {
    offlineStorageMock.getSyncQueue.and.returnValue(Promise.resolve([
      {
        localId: 13,
        operationId: 'op-13',
        workspaceId: 'ws-cairo',
        sessionId: 'sess-cancelled',
        studentId: 'std-13',
        identifierUsed: '04:AA:BB:CC',
        source: 'NfcCard',
        scannedAtUtc: '2026-10-21T08:25:00Z',
        syncStatus: 'Conflict',
        conflictReason: 'SessionCancelledOnline',
        retryCount: 0
      }
    ]));

    await service.resolveConflict('op-13', 'Void', 'إلغاء الحضور بطلب الإدارة');

    expect(offlineStorageMock.updateSyncStatus).toHaveBeenCalledWith(
      13,
      'Synced',
      'Voided: إلغاء الحضور بطلب الإدارة'
    );
  });
});
