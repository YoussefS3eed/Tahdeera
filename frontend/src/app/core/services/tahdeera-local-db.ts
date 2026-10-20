/**
 * Tahdeera Client-side IndexedDB Database
 * Powered by Dexie.js v4
 * Milestone F3: Local-first persistence engine with offline queueing & conflict tracking
 */

import Dexie, { Table } from 'dexie';
import { Student, ClassSession, AttendanceRecord } from '../models/domain.models';
import { LocalSyncOperation, WorkspaceMetadataRecord } from '../models/sync.models';

export interface LocalAttendanceRow {
  localId?: number;
  operationId: string;
  workspaceId: string;
  sessionId: string;
  studentId: string;
  studentCode: string;
  studentName: string;
  identifier: string;
  method: string;
  outcome: string;
  syncStatus: 'Pending' | 'Syncing' | 'Synced' | 'Conflict';
  conflictReason?: string;
  scannedAtUtc: string;
}

export class TahdeeraLocalDb extends Dexie {
  cached_students!: Table<any, string>;
  cached_sessions!: Table<any, string>;
  cached_attendance!: Table<any, string>;
  sync_queue!: Table<any, number>;
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

    this.version(2).stores({
      cached_students: 'id, workspaceId, [workspaceId+studentCode], nfcCardUid, studentCode, qrIdentifierToken, fullName, *groupIds',
      cached_sessions: 'id, workspaceId, groupId, status, startsAt, endsAt',
      cached_attendance: 'id, sessionId, studentId, [sessionId+studentId], status, markedAtUtc',
      sync_queue: '++localId, operationId, deviceGuid, workspaceId, sessionId, studentId, status, syncStatus, conflictReason, scannedAtUtc, [workspaceId+status]',
      workspace_metadata: 'key'
    });
  }
}
