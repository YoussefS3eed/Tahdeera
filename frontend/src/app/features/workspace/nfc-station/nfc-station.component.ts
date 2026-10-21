import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  WritableSignal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { IdentificationPipelineService } from '../../../core/services/identification-pipeline.service';
import { NfcWedgeAdapterService } from '../../../core/services/nfc-wedge-adapter.service';
import { WebNfcAdapterService } from '../../../core/services/web-nfc-adapter.service';
import { AudioFeedbackService } from '../../../core/services/audio-feedback.service';
import { OfflineStorageService } from '../../../core/services/offline-storage.service';
import { AttendanceSyncService } from '../../../core/services/attendance-sync.service';
import { PostSyncReviewModalComponent, ConflictedSyncItem } from '../attendance/components/post-sync-review-modal/post-sync-review-modal.component';
import { LastScanCardComponent } from './components/last-scan-card/last-scan-card.component';
import { CrossGroupDrawerComponent } from './components/cross-group-drawer/cross-group-drawer.component';
import { QrScannerComponent } from '../../../shared/components/qr-scanner/qr-scanner.component';
import {
  ClassSessionItem,
  CredentialScanResult
} from '../../../core/models/academic.models';

@Component({
  selector: 'app-nfc-station',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LastScanCardComponent,
    CrossGroupDrawerComponent,
    QrScannerComponent,
    PostSyncReviewModalComponent
  ],
  templateUrl: './nfc-station.component.html',
  styleUrls: ['./nfc-station.component.scss']
})
export class NfcStationComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);
  readonly pipeline = inject(IdentificationPipelineService);
  readonly nfcWedge = inject(NfcWedgeAdapterService);
  readonly webNfc = inject(WebNfcAdapterService);
  readonly audio = inject(AudioFeedbackService);
  readonly offlineStorage = inject(OfflineStorageService);
  readonly syncService = inject(AttendanceSyncService);

  readonly sessions: WritableSignal<ClassSessionItem[]> = signal<ClassSessionItem[]>([]);
  readonly selectedSessionId: WritableSignal<string> = signal<string>('');
  readonly lastScanResult: WritableSignal<CredentialScanResult | null> = signal<CredentialScanResult | null>(null);

  readonly isQrScannerOpen: WritableSignal<boolean> = signal<boolean>(false);
  readonly isCrossGroupDrawerOpen: WritableSignal<boolean> = signal<boolean>(false);
  readonly crossGroupScan: WritableSignal<CredentialScanResult | null> = signal<CredentialScanResult | null>(null);

  readonly manualSearchQuery = signal<string>('');
  readonly isSyncing = signal<boolean>(false);
  readonly showConflictModal: WritableSignal<boolean> = signal<boolean>(false);
  readonly conflictedItems: WritableSignal<ConflictedSyncItem[]> = signal<ConflictedSyncItem[]>([]);

  private subscriptions: Subscription = new Subscription();

  ngOnInit(): void {
    this.loadSessions();

    // Listen to hardware USB wedge scans
    this.subscriptions.add(
      this.nfcWedge.uidScanned$.subscribe(uid => {
        this.handleScan(uid, 'UsbHidWedge');
      })
    );

    // Listen to Mobile Web NFC scans
    this.subscriptions.add(
      this.webNfc.tagScanned$.subscribe(uid => {
        this.handleScan(uid, 'WebNfc');
      })
    );

    // Listen to pipeline results
    this.subscriptions.add(
      this.pipeline.scanResult$.subscribe(res => {
        this.lastScanResult.set(res);
        if (res.outcome === 'WrongGroup') {
          this.crossGroupScan.set(res);
          this.isCrossGroupDrawerOpen.set(true);
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  loadSessions(): void {
    this.http.get<ClassSessionItem[]>('/api/v1/schedule/sessions').subscribe({
      next: data => {
        this.sessions.set(data);
        const running = data.find(s => s.status === 'Running' || !s.isCancelled);
        if (running) {
          this.selectedSessionId.set(running.id);
        } else if (data.length > 0) {
          this.selectedSessionId.set(data[0].id);
        }
      },
      error: err => console.error('Failed to load sessions', err)
    });
  }

  async handleScan(identifier: string, method: 'UsbHidWedge' | 'WebNfc' | 'CameraQr' | 'ManualSearch'): Promise<void> {
    const sessionId = this.selectedSessionId();
    if (!sessionId) {
      alert('يرجى اختيار الحصة الجارية أولاً');
      return;
    }
    await this.pipeline.processIdentifier(sessionId, identifier, method);
  }

  onManualSearchSubmit(): void {
    const q = this.manualSearchQuery().trim();
    if (q) {
      this.handleScan(q, 'ManualSearch');
      this.manualSearchQuery.set('');
    }
  }

  openQrScanner(): void {
    this.isQrScannerOpen.set(true);
  }

  closeQrScanner(): void {
    this.isQrScannerOpen.set(false);
  }

  onQrDecoded(token: string): void {
    this.closeQrScanner();
    this.handleScan(token, 'CameraQr');
  }

  async toggleWebNfc(): Promise<void> {
    if (this.webNfc.isScanning()) {
      this.webNfc.stopScanning();
    } else {
      await this.webNfc.startScanning();
    }
  }

  toggleAudio(): void {
    this.audio.toggleMute();
  }

  async syncNow(): Promise<void> {
    this.isSyncing.set(true);
    try {
      const res = await this.syncService.syncPending();
      await this.offlineStorage.refreshPendingCount();
      if (res.conflicts > 0) {
        await this.openConflicts();
      }
    } catch (err) {
      console.error('Station sync failed', err);
    } finally {
      this.isSyncing.set(false);
    }
  }

  async openConflicts(): Promise<void> {
    const rawConflicts = await this.syncService.getConflictedItems();
    if (rawConflicts && rawConflicts.length > 0) {
      const mapped: ConflictedSyncItem[] = rawConflicts.map(item => ({
        operationId: item.operationId,
        workspaceId: item.workspaceId,
        sessionId: item.sessionId,
        studentId: item.studentId,
        identifier: item.identifier,
        method: item.method,
        scannedAtUtc: item.scannedAtUtc,
        syncStatus: 'Conflict',
        conflictReason: item.conflictReason || 'SessionCancelledOnline',
        studentName: item.studentName || 'طالب مسجل محلياً',
        studentCode: item.studentCode || 'ST-OFFLINE'
      }));
      this.conflictedItems.set(mapped);
    }
    this.showConflictModal.set(true);
  }

  resolveConflictItem(event: { operationId: string; decision: 'ApproveExceptional' | 'Void'; justificationReason?: string }): void {
    this.syncService.resolveConflict(event.operationId, event.decision, event.justificationReason);
    this.conflictedItems.update(prev => prev.filter(i => i.operationId !== event.operationId));
    if (this.conflictedItems().length === 0) {
      this.showConflictModal.set(false);
    }
  }

  async onConfirmCrossGroup(reason: string): Promise<void> {
    const scan = this.crossGroupScan();
    if (scan && scan.student) {
      await this.pipeline.admitExceptionalSubstitute(scan.student.id, this.selectedSessionId(), reason);
      this.isCrossGroupDrawerOpen.set(false);
      this.crossGroupScan.set(null);
    }
  }

  onCancelCrossGroup(): void {
    this.isCrossGroupDrawerOpen.set(false);
    this.crossGroupScan.set(null);
  }

  getSelectedSession(): ClassSessionItem | undefined {
    return this.sessions().find(s => s.id === this.selectedSessionId());
  }
}
