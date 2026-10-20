/**
 * Foundation Test Harness Component
 * Milestone F1 Phase 8: Operational Verification Cockpit for All Foundation Services & UI Tokens
 */

import {
  Component,
  OnInit,
  OnDestroy,
  signal,
  WritableSignal,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subscription } from 'rxjs';

import {
  ThemeService,
  LocalizationService,
  AudioFeedbackService,
  NfcListenerService,
  OfflineStorageService,
  ScanOutcome,
  WorkspaceRole,
  SyncAction,
  SyncEntityType,
  SyncOperationStatus,
  AttendanceStatus,
  ProblemDetails,
  PagedResult,
  Student,
  ThemeMode,
  SupportedLanguage
} from '../../core';

import { FocusTrapDirective, SkeletonLoaderComponent } from '../../shared';
import { TranslatePipe } from '../../core/pipes/translate.pipe';

@Component({
  selector: 'app-foundation-harness',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    TranslatePipe,
    FocusTrapDirective,
    SkeletonLoaderComponent
  ],
  templateUrl: './foundation-harness.component.html',
  styleUrls: ['./foundation-harness.component.css']
})
export class FoundationHarnessComponent implements OnInit, OnDestroy {
  // Injected Foundation Services
  readonly themeService = inject(ThemeService);
  readonly localizationService = inject(LocalizationService);
  readonly audioService = inject(AudioFeedbackService);
  readonly nfcService = inject(NfcListenerService);
  readonly storageService = inject(OfflineStorageService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  // Local Reactive Signals
  readonly deviceGuid: WritableSignal<string> = signal<string>('Loading...');
  readonly cachedStudentCount: WritableSignal<number> = signal<number>(0);
  readonly recentScans: WritableSignal<Array<{ uid: string; timestamp: number }>> = signal([]);
  readonly isModalOpen: WritableSignal<boolean> = signal<boolean>(false);
  readonly apiLatency: WritableSignal<number | null> = signal<number | null>(null);
  readonly apiResult: WritableSignal<string | null> = signal<string | null>(null);
  readonly apiError: WritableSignal<ProblemDetails | null> = signal<ProblemDetails | null>(null);
  readonly isApiLoading: WritableSignal<boolean> = signal<boolean>(false);

  // Scan simulation model
  simulatedCardUid = '04A27C19';

  // Subscriptions
  private scanSub?: Subscription;

  // Enums for template access
  readonly ScanOutcome = ScanOutcome;

  async ngOnInit(): Promise<void> {
    // 1. Start hardware wedge listener
    this.nfcService.startListening();

    // 2. Subscribe to real-time NFC scan events
    this.scanSub = this.nfcService.scanEvents$.subscribe(event => {
      this.recentScans.update(scans => [
        { uid: event.uid, timestamp: event.timestamp },
        ...scans.slice(0, 9)
      ]);
      // Play audio based on simulated card
      if (event.uid === '04A27C19') {
        this.audioService.playOutcome(ScanOutcome.Present);
      } else {
        this.audioService.playOutcome(ScanOutcome.Late);
      }
    });

    // 3. Load device GUID and cached counts from Dexie
    try {
      const guid = await this.storageService.getDeviceGuid();
      this.deviceGuid.set(guid);
      await this.refreshStorageStats();
    } catch (err) {
      console.warn('[FoundationHarness] Error reading initial Dexie state:', err);
    }
  }

  ngOnDestroy(): void {
    if (this.scanSub) {
      this.scanSub.unsubscribe();
    }
  }

  // ==========================================
  // Theming Actions
  // ==========================================
  setTheme(mode: 'light' | 'dark' | 'system'): void {
    this.themeService.setTheme(mode as ThemeMode);
  }

  // ==========================================
  // Localization Actions
  // ==========================================
  setLanguage(lang: 'ar' | 'en'): void {
    this.localizationService.setLanguage(lang as SupportedLanguage);
  }

  // ==========================================
  // Audio Synthesis Actions
  // ==========================================
  playTone(outcome: ScanOutcome): void {
    this.audioService.playOutcome(outcome);
  }

  // ==========================================
  // NFC Hardware Simulation Actions
  // ==========================================
  simulateCardScan(): void {
    if (!this.simulatedCardUid.trim()) return;
    this.nfcService.simulateScan(this.simulatedCardUid.trim());
  }

  // ==========================================
  // Dexie Offline DB Actions
  // ==========================================
  async seedMockStudents(): Promise<void> {
    const mockStudents: Student[] = Array.from({ length: 15 }, (_, i) => ({
      id: `st-${(i + 1).toString().padStart(3, '0')}`,
      workspaceId: 'ws-cairo-01',
      studentCode: `ST-${1000 + i + 1}`,
      fullName: `طالب تجريبي رقم ${i + 1}`,
      guardianPhone: `010123456${(10 + i).toString().substring(0, 2)}`,
      nfcCardUid: `04A27C${(19 + i).toString(16).toUpperCase()}`,
      currentBalance: i % 3 === 0 ? 50 : 0,
      isActive: true,
      createdAtUtc: new Date().toISOString()
    }));

    await this.storageService.cacheStudents(mockStudents);
    await this.refreshStorageStats();
  }

  async simulateOfflineAttendance(): Promise<void> {
    await this.storageService.enqueueSyncOperation({
      workspaceId: 'ws-cairo-01',
      userId: 'usr-001',
      entityType: SyncEntityType.Attendance,
      entityId: `st-001`,
      action: SyncAction.Create,
      payload: {
        sessionId: 'sess-001',
        status: AttendanceStatus.Present,
        scannedAtUtc: new Date().toISOString()
      }
    });
  }

  async clearWorkspaceCache(): Promise<void> {
    await this.storageService.clearWorkspaceData('ws-cairo-01');
    await this.refreshStorageStats();
  }

  private async refreshStorageStats(): Promise<void> {
    if (this.storageService.isStorageAvailable()) {
      const count = await this.storageService.db.cached_students
        .where('workspaceId')
        .equals('ws-cairo-01')
        .count();
      this.cachedStudentCount.set(count);
      await this.storageService.refreshPendingCount('ws-cairo-01');
    }
  }

  // ==========================================
  // Mock API Telemetry Actions
  // ==========================================
  fetchStudents(pageNumber: number): void {
    this.isApiLoading.set(true);
    this.apiError.set(null);
    const start = performance.now();

    this.http.get<PagedResult<Student>>(`/api/v1/students?pageNumber=${pageNumber}&pageSize=5`).subscribe({
      next: (res) => {
        const elapsed = Math.round(performance.now() - start);
        this.apiLatency.set(elapsed);
        this.apiResult.set(JSON.stringify(res, null, 2));
        this.isApiLoading.set(false);
      },
      error: (err) => {
        const elapsed = Math.round(performance.now() - start);
        this.apiLatency.set(elapsed);
        this.apiError.set(err.error);
        this.apiResult.set(null);
        this.isApiLoading.set(false);
      }
    });
  }

  triggerConflict(): void {
    this.isApiLoading.set(true);
    this.apiResult.set(null);
    const start = performance.now();

    this.http.post('/api/v1/attendance?simulateConflict=true', {
      sessionId: 'sess-001',
      studentId: 'st-001'
    }).subscribe({
      next: () => {
        this.isApiLoading.set(false);
      },
      error: (err) => {
        const elapsed = Math.round(performance.now() - start);
        this.apiLatency.set(elapsed);
        this.apiError.set(err.error);
        this.isApiLoading.set(false);
      }
    });
  }

  // ==========================================
  // Modal & Focus Trap Actions
  // ==========================================
  openModal(): void {
    this.isModalOpen.set(true);
  }

  closeModal(): void {
    this.isModalOpen.set(false);
  }
}
