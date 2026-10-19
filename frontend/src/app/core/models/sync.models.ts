/**
 * Tahdeera Local-First Sync Models
 * Authoritative interfaces from data-model.md
 */

import { SyncAction, SyncEntityType, SyncOperationStatus } from '../enums/domain.enums';

export interface LocalSyncOperation {
  localId?: number;              // Dexie auto-increment primary key
  operationId: string;           // Unique UUID v4 idempotency key
  deviceGuid: string;            // Persistent local browser device GUID
  workspaceId: string;           // Target workspace identifier
  userId: string;                // User who recorded the action
  entityType: SyncEntityType;    // 'Attendance' | 'Payment' | 'Student' | 'Session'
  entityId: string;              // ID of affected entity
  action: SyncAction;            // 'Create' | 'Update' | 'Reverse'
  payload: any;                  // Operation payload (e.g., student ID, marked status)
  clientTimestampUtc: string;    // Exact ISO 8601 UTC scan/recording timestamp
  status: SyncOperationStatus;   // 'PendingSync' | 'Syncing' | 'Synced' | 'Conflict' | 'Failed'
  retryCount: number;            // Number of retry attempts
  lastError?: string;            // Error message from last sync attempt
}

export interface OfflineSyncState {
  isOnline: boolean;
  pendingCount: number;
  syncedCount: number;
  conflictCount: number;
  failedCount: number;
  isSyncing: boolean;
  lastSyncTimeUtc?: string;
}

export interface WorkspaceMetadataRecord {
  key: string;                   // e.g., 'active_workspace', 'last_sync_timestamp', 'device_guid'
  value: any;
  updatedAtUtc: string;
}
