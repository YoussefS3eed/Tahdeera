/**
 * Tahdeera Offline Storage Service
 * Milestone F1 Phase 6: Dexie.js transactional IndexedDB persistence layer
 */

import { Injectable, signal, WritableSignal } from '@angular/core';
import { TahdeeraLocalDb } from './tahdeera-local-db';
import { Student, ClassSession, AttendanceRecord } from '../models/domain.models';
import { LocalSyncOperation, WorkspaceMetadataRecord } from '../models/sync.models';
import { SyncOperationStatus } from '../enums/domain.enums';

export type EnqueueSyncOperationInput = Omit<
  LocalSyncOperation,
  'localId' | 'operationId' | 'deviceGuid' | 'clientTimestampUtc' | 'status' | 'retryCount'
> & Partial<Pick<LocalSyncOperation, 'operationId' | 'deviceGuid' | 'clientTimestampUtc' | 'status' | 'retryCount'>>;

@Injectable({
  providedIn: 'root'
})
export class OfflineStorageService {
  readonly db: TahdeeraLocalDb;

  // Reactive Signals
  readonly isStorageAvailable: WritableSignal<boolean> = signal<boolean>(false);
  readonly pendingSyncCount: WritableSignal<number> = signal<number>(0);
  readonly isDbReady: WritableSignal<boolean> = signal<boolean>(false);

  constructor() {
    this.db = new TahdeeraLocalDb();
    this.initialize();
  }

  /**
   * Initializes IndexedDB database, sets status signals, and validates device GUID.
   */
  private async initialize(): Promise<void> {
    try {
      if (typeof indexedDB === 'undefined') {
        throw new Error('IndexedDB not supported in current environment');
      }
      await this.db.open();
      this.isStorageAvailable.set(true);
      await this.getDeviceGuid();
      await this.refreshPendingCount();
    } catch (err) {
      console.warn('[OfflineStorageService] IndexedDB unavailable or restricted, running in fallback mode:', err);
      this.isStorageAvailable.set(false);
    } finally {
      this.isDbReady.set(true);
    }
  }

  /**
   * Refreshes the pending synchronization count Signal.
   */
  async refreshPendingCount(workspaceId?: string): Promise<number> {
    if (!this.isStorageAvailable()) {
      return 0;
    }
    try {
      let count = 0;
      if (workspaceId) {
        count = await this.db.sync_queue
          .where('[workspaceId+status]')
          .equals([workspaceId, SyncOperationStatus.PendingSync])
          .count();
      } else {
        count = await this.db.sync_queue
          .where('status')
          .equals(SyncOperationStatus.PendingSync)
          .count();
      }
      this.pendingSyncCount.set(count);
      return count;
    } catch (err) {
      console.error('[OfflineStorageService] Error refreshing pending count:', err);
      return 0;
    }
  }

  // ==========================================
  // Sync Queue Operations
  // ==========================================

  /**
   * Enqueues an offline mutation into the persistent sync_queue.
   * Returns assigned auto-increment localId.
   */
  async enqueueSyncOperation(operation: EnqueueSyncOperationInput): Promise<number> {
    if (!this.isStorageAvailable()) {
      return -1;
    }

    const deviceGuid = operation.deviceGuid || (await this.getDeviceGuid());
    const op: LocalSyncOperation = {
      ...operation,
      operationId: operation.operationId || this.generateUuid(),
      deviceGuid,
      clientTimestampUtc: operation.clientTimestampUtc || new Date().toISOString(),
      status: operation.status || SyncOperationStatus.PendingSync,
      retryCount: operation.retryCount ?? 0
    };

    const localId = await this.db.sync_queue.add(op);
    await this.refreshPendingCount(operation.workspaceId);
    return localId;
  }

  /**
   * Retrieves pending sync operations in strict FIFO order (localId ASC).
   */
  async getPendingOperations(workspaceId?: string): Promise<LocalSyncOperation[]> {
    if (!this.isStorageAvailable()) {
      return [];
    }

    if (workspaceId) {
      return this.db.sync_queue
        .where('[workspaceId+status]')
        .equals([workspaceId, SyncOperationStatus.PendingSync])
        .sortBy('localId');
    }

    return this.db.sync_queue
      .where('status')
      .equals(SyncOperationStatus.PendingSync)
      .sortBy('localId');
  }

  /**
   * Updates status and error information for a specific sync queue operation.
   */
  async updateOperationStatus(localId: number, status: SyncOperationStatus, error?: string): Promise<void> {
    if (!this.isStorageAvailable()) {
      return;
    }

    const updates: Partial<LocalSyncOperation> = { status };
    if (error !== undefined) {
      updates.lastError = error;
    }
    if (status === SyncOperationStatus.Failed) {
      const existing = await this.db.sync_queue.get(localId);
      if (existing) {
        updates.retryCount = (existing.retryCount || 0) + 1;
      }
    }

    await this.db.sync_queue.update(localId, updates);
    await this.refreshPendingCount();
  }

  /**
   * Marks a batch of local operations as successfully synced.
   */
  async markOperationsSynced(localIds: number[]): Promise<void> {
    if (!this.isStorageAvailable() || localIds.length === 0) {
      return;
    }

    await this.db.transaction('rw', this.db.sync_queue, async () => {
      for (const id of localIds) {
        await this.db.sync_queue.update(id, { status: SyncOperationStatus.Synced });
      }
    });

    await this.refreshPendingCount();
  }

  // ==========================================
  // Student Cache Operations
  // ==========================================

  /**
   * Caches a batch of student records locally.
   */
  async cacheStudents(students: Student[]): Promise<void> {
    if (!this.isStorageAvailable() || students.length === 0) {
      return;
    }
    await this.db.cached_students.bulkPut(students);
  }

  /**
   * Performs an indexed lookup for a student by card UID within the active workspace.
   */
  async findStudentByNfcUid(nfcUid: string, workspaceId: string): Promise<Student | undefined> {
    if (!this.isStorageAvailable()) {
      return undefined;
    }
    const normalized = nfcUid.replace(/[\s:-]/g, '').toUpperCase();
    return this.db.cached_students
      .where('nfcCardUid')
      .equals(normalized)
      .and((s: Student) => s.workspaceId === workspaceId && s.isActive)
      .first();
  }

  /**
   * Performs a compound indexed lookup for a student by studentCode within the active workspace.
   */
  async findStudentByCode(studentCode: string, workspaceId: string): Promise<Student | undefined> {
    if (!this.isStorageAvailable()) {
      return undefined;
    }
    return this.db.cached_students
      .where('[workspaceId+studentCode]')
      .equals([workspaceId, studentCode])
      .first();
  }

  /**
   * Gets a student by primary ID.
   */
  async getStudentById(id: string): Promise<Student | undefined> {
    if (!this.isStorageAvailable()) return undefined;
    return this.db.cached_students.get(id);
  }

  // ==========================================
  // Session Cache Operations
  // ==========================================

  /**
   * Caches class sessions locally.
   */
  async cacheSessions(sessions: ClassSession[]): Promise<void> {
    if (!this.isStorageAvailable() || sessions.length === 0) {
      return;
    }
    await this.db.cached_sessions.bulkPut(sessions);
  }

  /**
   * Looks up an active session within the given workspace.
   */
  async getActiveSession(workspaceId: string, nowUtc?: string): Promise<ClassSession | undefined> {
    if (!this.isStorageAvailable()) {
      return undefined;
    }
    const current = nowUtc || new Date().toISOString();
    return this.db.cached_sessions
      .where('workspaceId')
      .equals(workspaceId)
      .and((s: ClassSession) => s.status === 'Active' && s.startsAt <= current && s.endsAt >= current)
      .first();
  }

  /**
   * Gets a session by primary ID.
   */
  async getSessionById(id: string): Promise<ClassSession | undefined> {
    if (!this.isStorageAvailable()) return undefined;
    return this.db.cached_sessions.get(id);
  }

  // ==========================================
  // Attendance Cache Operations
  // ==========================================

  /**
   * Inserts or updates an attendance record in the local cache.
   */
  async cacheAttendanceRecord(record: AttendanceRecord): Promise<void> {
    if (!this.isStorageAvailable()) {
      return;
    }
    await this.db.cached_attendance.put(record);
  }

  /**
   * Checks whether a student has already been checked into a session (compound index [sessionId+studentId]).
   */
  async findAttendanceByStudentAndSession(studentId: string, sessionId: string): Promise<AttendanceRecord | undefined> {
    if (!this.isStorageAvailable()) {
      return undefined;
    }
    return this.db.cached_attendance
      .where('[sessionId+studentId]')
      .equals([sessionId, studentId])
      .first();
  }

  /**
   * Retrieves all cached attendance records for a specific session.
   */
  async getAttendanceForSession(sessionId: string): Promise<AttendanceRecord[]> {
    if (!this.isStorageAvailable()) {
      return [];
    }
    return this.db.cached_attendance
      .where('sessionId')
      .equals(sessionId)
      .toArray();
  }

  // ==========================================
  // Workspace Metadata & Isolation
  // ==========================================

  /**
   * Retrieves or creates a persistent device GUID in workspace_metadata.
   */
  async getDeviceGuid(): Promise<string> {
    if (!this.isStorageAvailable()) {
      return 'fallback-device-guid';
    }
    const existing = await this.db.workspace_metadata.get('device_guid');
    if (existing && existing.value) {
      return existing.value as string;
    }
    const newGuid = this.generateUuid();
    await this.db.workspace_metadata.put({
      key: 'device_guid',
      value: newGuid,
      updatedAtUtc: new Date().toISOString()
    });
    return newGuid;
  }

  /**
   * Gets a typed metadata record.
   */
  async getMetadata<T>(key: string): Promise<T | null> {
    if (!this.isStorageAvailable()) return null;
    const record = await this.db.workspace_metadata.get(key);
    return record ? (record.value as T) : null;
  }

  /**
   * Sets a typed metadata record.
   */
  async setMetadata<T>(key: string, value: T): Promise<void> {
    if (!this.isStorageAvailable()) return;
    await this.db.workspace_metadata.put({
      key,
      value,
      updatedAtUtc: new Date().toISOString()
    });
  }

  /**
   * Purges cached students and sessions for a specific workspace on logout/switch.
   * Strictly preserves sync_queue to avoid losing pending mutations.
   */
  async clearWorkspaceData(workspaceId: string): Promise<void> {
    if (!this.isStorageAvailable()) return;
    await this.db.transaction('rw', [this.db.cached_students, this.db.cached_sessions, this.db.cached_attendance], async () => {
      await this.db.cached_students.where('workspaceId').equals(workspaceId).delete();
      const sessions = await this.db.cached_sessions.where('workspaceId').equals(workspaceId).toArray();
      const sessionIds = sessions.map(s => s.id);
      await this.db.cached_sessions.where('workspaceId').equals(workspaceId).delete();
      for (const sId of sessionIds) {
        await this.db.cached_attendance.where('sessionId').equals(sId).delete();
      }
    });
  }

  /**
   * Resets all tables in TahdeeraLocalDb (used for test teardown).
   */
  async resetDatabase(): Promise<void> {
    if (!this.isStorageAvailable()) return;
    await this.db.transaction('rw', [
      this.db.cached_students,
      this.db.cached_sessions,
      this.db.cached_attendance,
      this.db.sync_queue,
      this.db.workspace_metadata
    ], async () => {
      await this.db.cached_students.clear();
      await this.db.cached_sessions.clear();
      await this.db.cached_attendance.clear();
      await this.db.sync_queue.clear();
      await this.db.workspace_metadata.clear();
    });
    this.pendingSyncCount.set(0);
  }

  /**
   * Safe UUID generator with fallback for non-secure HTTP contexts.
   */
  private generateUuid(): string {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
      return crypto.randomUUID();
    }
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  }
}
