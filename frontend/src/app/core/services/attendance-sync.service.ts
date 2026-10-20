import { Injectable, inject, signal } from '@angular/core';
import { OfflineStorageService } from './offline-storage.service';
import { AcademicService } from './academic.service';
import { SyncBatchRequest, SyncBatchItemPayload, LocalAttendanceRow } from '../models/academic.models';
import { firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AttendanceSyncService {
  private readonly offlineStorage = inject(OfflineStorageService);
  private readonly academicService = inject(AcademicService);

  readonly isSyncing = signal(false);
  readonly pendingCount = signal(0);
  readonly conflictCount = signal(0);

  constructor() {
    this.refreshCounts();
  }

  async refreshCounts(): Promise<void> {
    const queue: LocalAttendanceRow[] = await this.offlineStorage.getSyncQueue();
    const pending = queue.filter((item: LocalAttendanceRow) => item.syncStatus === 'Pending' || item.syncStatus === 'Syncing').length;
    const conflicts = queue.filter((item: LocalAttendanceRow) => item.syncStatus === 'Conflict').length;
    this.pendingCount.set(pending);
    this.conflictCount.set(conflicts);
  }

  async syncPending(workspaceId = 'ws-cairo-01'): Promise<{ synced: number; conflicts: number }> {
    if (this.isSyncing()) {
      return { synced: 0, conflicts: 0 };
    }

    this.isSyncing.set(true);

    try {
      const queue: LocalAttendanceRow[] = await this.offlineStorage.getSyncQueue();
      const itemsToSync = queue.filter((item: LocalAttendanceRow) => item.syncStatus === 'Pending' || item.syncStatus === 'Syncing');

      if (itemsToSync.length === 0) {
        await this.refreshCounts();
        this.isSyncing.set(false);
        return { synced: 0, conflicts: 0 };
      }

      const payload: SyncBatchRequest = {
        workspaceId,
        items: itemsToSync.map((item: LocalAttendanceRow): SyncBatchItemPayload => ({
          operationId: item.operationId,
          sessionId: item.sessionId,
          studentId: item.studentId,
          identifierUsed: item.identifier,
          scannedAtUtc: item.scannedAtUtc,
          source: item.method
        }))
      };

      const response = await firstValueFrom(this.academicService.syncBatch(payload));

      let syncedCount = 0;
      let conflictCount = 0;

      for (const resItem of response.results) {
        const local = itemsToSync.find((i: LocalAttendanceRow) => i.operationId === resItem.operationId);
        if (local && local.localId) {
          if (resItem.status === 'Synced') {
            await this.offlineStorage.updateSyncStatus(local.localId, 'Synced');
            syncedCount++;
          } else if (resItem.status === 'Conflict') {
            await this.offlineStorage.updateSyncStatus(local.localId, 'Conflict', resItem.conflictReason);
            conflictCount++;
          }
        }
      }

      await this.refreshCounts();
      return { synced: syncedCount, conflicts: conflictCount };
    } catch (err) {
      console.error('Batch sync failed:', err);
      return { synced: 0, conflicts: 0 };
    } finally {
      this.isSyncing.set(false);
    }
  }

  async resolveConflict(
    operationId: string,
    decision: 'ApproveExceptional' | 'Void',
    justificationReason?: string
  ): Promise<void> {
    const queue: LocalAttendanceRow[] = await this.offlineStorage.getSyncQueue();
    const target = queue.find((i: LocalAttendanceRow) => i.operationId === operationId);
    if (!target || !target.localId) return;

    if (decision === 'ApproveExceptional') {
      await this.offlineStorage.updateSyncStatus(target.localId, 'Synced');
    } else {
      await this.offlineStorage.updateSyncStatus(target.localId, 'Synced', `Voided: ${justificationReason || 'User action'}`);
    }

    await this.refreshCounts();
  }
}
