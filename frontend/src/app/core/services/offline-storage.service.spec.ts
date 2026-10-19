/**
 * Unit Tests for OfflineStorageService
 * Milestone F1 Phase 6: Dexie.js Offline Storage Database & Persistence Engine
 */

import { TestBed } from '@angular/core/testing';
import { OfflineStorageService } from './offline-storage.service';
import { SyncAction, SyncEntityType, SyncOperationStatus, AttendanceStatus } from '../enums/domain.enums';
import { Student, ClassSession, AttendanceRecord } from '../models/domain.models';

describe('OfflineStorageService', () => {
  let service: OfflineStorageService;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [OfflineStorageService]
    });
    service = TestBed.inject(OfflineStorageService);
    // Ensure clean state before each test
    await service.resetDatabase();
  });

  afterEach(async () => {
    await service.resetDatabase();
  });

  it('should initialize and report storage availability', () => {
    expect(service).toBeTruthy();
    expect(service.isDbReady()).toBeTrue();
    expect(service.isStorageAvailable()).toBeTrue();
    expect(service.pendingSyncCount()).toBe(0);
  });

  it('should generate and persist a valid device_guid', async () => {
    const guid1 = await service.getDeviceGuid();
    expect(guid1).toBeTruthy();
    expect(guid1.length).toBeGreaterThan(20);

    const guid2 = await service.getDeviceGuid();
    expect(guid2).toBe(guid1); // Persistent across calls
  });

  it('should enqueue sync operations, increment count, and retrieve in strict FIFO order', async () => {
    const op1Id = await service.enqueueSyncOperation({
      workspaceId: 'ws-1',
      userId: 'user-1',
      entityType: SyncEntityType.Attendance,
      entityId: 'student-1',
      action: SyncAction.Create,
      payload: { sessionId: 'sess-1', status: 'Present' },
      status: SyncOperationStatus.PendingSync,
      clientTimestampUtc: '2026-10-20T01:00:00Z',
      retryCount: 0
    });

    const op2Id = await service.enqueueSyncOperation({
      workspaceId: 'ws-1',
      userId: 'user-1',
      entityType: SyncEntityType.Attendance,
      entityId: 'student-2',
      action: SyncAction.Create,
      payload: { sessionId: 'sess-1', status: 'Late' },
      status: SyncOperationStatus.PendingSync,
      clientTimestampUtc: '2026-10-20T01:05:00Z',
      retryCount: 0
    });

    expect(op1Id).toBeGreaterThan(0);
    expect(op2Id).toBeGreaterThan(op1Id);
    expect(service.pendingSyncCount()).toBe(2);

    const pending = await service.getPendingOperations('ws-1');
    expect(pending.length).toBe(2);
    expect(pending[0].localId).toBe(op1Id);
    expect(pending[1].localId).toBe(op2Id);
    expect(pending[0].entityId).toBe('student-1');
    expect(pending[1].entityId).toBe('student-2');
  });

  it('should update operation status and retry count on failure', async () => {
    const localId = await service.enqueueSyncOperation({
      workspaceId: 'ws-1',
      userId: 'user-1',
      entityType: SyncEntityType.Attendance,
      entityId: 'student-1',
      action: SyncAction.Create,
      payload: {},
      status: SyncOperationStatus.PendingSync,
      clientTimestampUtc: new Date().toISOString(),
      retryCount: 0
    });

    await service.updateOperationStatus(localId, SyncOperationStatus.Failed, 'Network timeout');
    const record = await service.db.sync_queue.get(localId);

    expect(record?.status).toBe(SyncOperationStatus.Failed);
    expect(record?.lastError).toBe('Network timeout');
    expect(record?.retryCount).toBe(1);
  });

  it('should mark operations as synced and decrement pending count', async () => {
    const localId = await service.enqueueSyncOperation({
      workspaceId: 'ws-1',
      userId: 'user-1',
      entityType: SyncEntityType.Attendance,
      entityId: 'student-1',
      action: SyncAction.Create,
      payload: {},
      status: SyncOperationStatus.PendingSync,
      clientTimestampUtc: new Date().toISOString(),
      retryCount: 0
    });

    expect(service.pendingSyncCount()).toBe(1);

    await service.markOperationsSynced([localId]);
    expect(service.pendingSyncCount()).toBe(0);

    const record = await service.db.sync_queue.get(localId);
    expect(record?.status).toBe(SyncOperationStatus.Synced);
  });

  it('should cache students and perform fast indexed lookups by card UID and student code', async () => {
    const students: Student[] = [
      {
        id: 'st-uuid-1',
        workspaceId: 'ws-1',
        studentCode: 'ST-1001',
        fullName: 'عمر خالد',
        guardianPhone: '01012345678',
        nfcCardUid: '04A27C19',
        currentBalance: 0,
        isActive: true,
        createdAtUtc: new Date().toISOString()
      },
      {
        id: 'st-uuid-2',
        workspaceId: 'ws-2', // Different workspace
        studentCode: 'ST-1001',
        fullName: 'طالب في سنتر آخر',
        guardianPhone: '01212345678',
        nfcCardUid: 'B1C2D3E4',
        currentBalance: 0,
        isActive: true,
        createdAtUtc: new Date().toISOString()
      }
    ];

    await service.cacheStudents(students);

    // Lookup by NFC UID with lowercase query and spaces
    const foundByNfc = await service.findStudentByNfcUid('04:a2:7c:19', 'ws-1');
    expect(foundByNfc).toBeDefined();
    expect(foundByNfc?.id).toBe('st-uuid-1');
    expect(foundByNfc?.fullName).toBe('عمر خالد');

    // Cross-workspace isolation check: UID in ws-1 does not resolve for ws-2
    const crossCheck = await service.findStudentByNfcUid('04A27C19', 'ws-2');
    expect(crossCheck).toBeUndefined();

    // Lookup by compound index [workspaceId+studentCode]
    const foundByCode = await service.findStudentByCode('ST-1001', 'ws-1');
    expect(foundByCode?.id).toBe('st-uuid-1');

    const foundByCodeWs2 = await service.findStudentByCode('ST-1001', 'ws-2');
    expect(foundByCodeWs2?.id).toBe('st-uuid-2');
  });

  it('should cache attendance records and guard against duplicate check-ins offline', async () => {
    const attendance: AttendanceRecord = {
      id: 'att-uuid-1',
      sessionId: 'sess-100',
      studentId: 'st-uuid-1',
      studentName: 'عمر خالد',
      studentCode: 'ST-1001',
      status: AttendanceStatus.Present,
      markedAtUtc: new Date().toISOString(),
      isAutomaticEvaluation: true,
      isLocked: false
    };

    await service.cacheAttendanceRecord(attendance);

    // Compound query [sessionId+studentId] finds existing record
    const existing = await service.findAttendanceByStudentAndSession('st-uuid-1', 'sess-100');
    expect(existing).toBeDefined();
    expect(existing?.status).toBe(AttendanceStatus.Present);

    // Unchecked student returns undefined
    const notScanned = await service.findAttendanceByStudentAndSession('st-uuid-999', 'sess-100');
    expect(notScanned).toBeUndefined();

    // Session attendance retrieval
    const sessionRecords = await service.getAttendanceForSession('sess-100');
    expect(sessionRecords.length).toBe(1);
    expect(sessionRecords[0].studentCode).toBe('ST-1001');
  });

  it('should cache class sessions and retrieve active sessions within time window', async () => {
    const now = new Date();
    const startsAt = new Date(now.getTime() - 30 * 60 * 1000).toISOString(); // 30 mins ago
    const endsAt = new Date(now.getTime() + 60 * 60 * 1000).toISOString();   // in 60 mins

    const sessions: ClassSession[] = [
      {
        id: 'sess-active',
        workspaceId: 'ws-1',
        groupId: 'grp-1',
        groupName: 'مجموعة الأحد 5 مساء',
        subjectName: 'رياضيات',
        startsAt,
        endsAt,
        status: 'Active'
      },
      {
        id: 'sess-future',
        workspaceId: 'ws-1',
        groupId: 'grp-2',
        groupName: 'مجموعة الإثنين',
        subjectName: 'فيزياء',
        startsAt: new Date(now.getTime() + 24 * 60 * 60 * 1000).toISOString(),
        endsAt: new Date(now.getTime() + 26 * 60 * 60 * 1000).toISOString(),
        status: 'Scheduled'
      }
    ];

    await service.cacheSessions(sessions);

    const active = await service.getActiveSession('ws-1', now.toISOString());
    expect(active).toBeDefined();
    expect(active?.id).toBe('sess-active');
  });

  it('should clear workspace cache upon switch while strictly preserving unsynced sync_queue', async () => {
    // 1. Add students to ws-1
    await service.cacheStudents([
      {
        id: 'st-ws1',
        workspaceId: 'ws-1',
        studentCode: 'ST-1001',
        fullName: 'طالب 1',
        guardianPhone: '01012345678',
        currentBalance: 0,
        isActive: true,
        createdAtUtc: new Date().toISOString()
      }
    ]);

    // 2. Add pending sync operation to ws-1
    const localId = await service.enqueueSyncOperation({
      workspaceId: 'ws-1',
      userId: 'user-1',
      entityType: SyncEntityType.Attendance,
      entityId: 'st-ws1',
      action: SyncAction.Create,
      payload: {},
      status: SyncOperationStatus.PendingSync,
      clientTimestampUtc: new Date().toISOString(),
      retryCount: 0
    });

    // 3. Clear ws-1 data (e.g. workspace logout/switch)
    await service.clearWorkspaceData('ws-1');

    // 4. Verify student cache was purged
    const foundStudent = await service.getStudentById('st-ws1');
    expect(foundStudent).toBeUndefined();

    // 5. Verify pending sync operation was PRESERVED
    const queueRecord = await service.db.sync_queue.get(localId);
    expect(queueRecord).toBeDefined();
    expect(queueRecord?.status).toBe(SyncOperationStatus.PendingSync);
    expect(service.pendingSyncCount()).toBe(1);
  });
});
