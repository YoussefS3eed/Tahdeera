/**
 * Tahdeera Client-side IndexedDB Database
 * Powered by Dexie.js v4
 * Milestone F1 Phase 6: Local-first persistence engine
 */

import Dexie, { Table } from 'dexie';
import { Student, ClassSession, AttendanceRecord } from '../models/domain.models';
import { LocalSyncOperation, WorkspaceMetadataRecord } from '../models/sync.models';

export class TahdeeraLocalDb extends Dexie {
  cached_students!: Table<Student, string>;
  cached_sessions!: Table<ClassSession, string>;
  cached_attendance!: Table<AttendanceRecord, string>;
  sync_queue!: Table<LocalSyncOperation, number>;
  workspace_metadata!: Table<WorkspaceMetadataRecord, string>;

  constructor() {
    super('TahdeeraLocalDb');
    this.version(1).stores({
      cached_students: 'id, workspaceId, [workspaceId+studentCode], nfcCardUid, studentCode, fullName',
      cached_sessions: 'id, workspaceId, groupId, [workspaceId+status], startsAt, endsAt',
      cached_attendance: 'id, sessionId, studentId, [sessionId+studentId], status, markedAtUtc',
      sync_queue: '++localId, operationId, deviceGuid, workspaceId, entityType, entityId, status, clientTimestampUtc, [workspaceId+status]',
      workspace_metadata: 'key'
    });
  }
}
