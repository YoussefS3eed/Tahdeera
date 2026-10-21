/**
 * Unified Student Identification Pipeline Orchestrator Service
 * Connects hardware inputs (USB HID Wedge, Web NFC, Camera QR, Manual Search),
 * verifies sessions and group enrollments, handles offline fallbacks via Dexie,
 * and triggers instant audio and visual feedback.
 * Reference: specs/010-f3-students-nfc-attendance/spec.md (DEC-05, DEC-06)
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject, firstValueFrom } from 'rxjs';
import { AudioFeedbackService } from './audio-feedback.service';
import { OfflineStorageService } from './offline-storage.service';
import {
  CredentialScanResult,
  ScanMethod
} from '../models/academic.models';

@Injectable({
  providedIn: 'root'
})
export class IdentificationPipelineService {
  private readonly http = inject(HttpClient);
  private readonly audioFeedback = inject(AudioFeedbackService);
  private readonly offlineStorage = inject(OfflineStorageService);

  private readonly scanResultSubject = new Subject<CredentialScanResult>();
  readonly scanResult$: Observable<CredentialScanResult> = this.scanResultSubject.asObservable();

  /**
   * Processes a scanned identifier (NFC UID, QR token, student code, or phone number)
   */
  async processIdentifier(
    sessionId: string,
    identifier: string,
    method: ScanMethod
  ): Promise<CredentialScanResult> {
    const payload = {
      sessionId,
      identifier: identifier.trim(),
      method,
      clientTimestampUtc: new Date().toISOString()
    };

    try {
      // 1. Try online API call
      const result = await firstValueFrom(
        this.http.post<CredentialScanResult>('/api/v1/attendance/scan', payload)
      );

      // Play synthesized audio tone natively
      this.audioFeedback.playOutcome(result.outcome);

      this.scanResultSubject.next(result);
      return result;
    } catch (err: any) {
      // 2. Offline Fallback via Dexie sync_queue
      console.warn('Network unavailable, recording scan offline in Dexie:', err);

      const isOfflineAvailable = this.offlineStorage.isStorageAvailable();
      if (isOfflineAvailable) {
        await this.offlineStorage.enqueueOfflineScan({
          operationId: crypto.randomUUID ? crypto.randomUUID() : `op-${Date.now()}`,
          workspaceId: 'ws-main',
          sessionId,
          studentId: 'offline-pending',
          studentCode: identifier,
          studentName: 'مسجل محلياً (دون اتصال)',
          identifier,
          method,
          outcome: 'Present',
          syncStatus: 'Pending',
          scannedAtUtc: new Date().toISOString()
        });
      }

      const offlineResult: CredentialScanResult = {
        outcome: 'Present',
        student: {
          id: 'offline-pending',
          studentCode: identifier,
          fullName: 'مسجل محلياً (دون اتصال)',
          guardianPhone: '',
          whatsappOptIn: false
        },
        scannedAtUtc: new Date().toISOString(),
        soundToPlay: 'PresentChime',
        messageAr: 'تم الحفظ في قائمة الانتظار المحلية (Offline)',
        messageEn: 'Recorded in Offline Queue'
      };

      this.audioFeedback.playPresentTone();
      this.scanResultSubject.next(offlineResult);
      return offlineResult;
    }
  }

  /**
   * Admits student from another group as exceptional substitute (DEC-06)
   */
  async admitExceptionalSubstitute(
    studentId: string,
    sessionId: string,
    reason: string
  ): Promise<any> {
    const payload = {
      recordId: `att-sub-${Date.now()}`,
      studentId,
      sessionId,
      newStatus: 'Present',
      substituteReason: reason || 'تحضير كبديل مجموعة استثنائي (DEC-06)',
      reason: reason || 'تحضير كبديل مجموعة استثنائي (DEC-06)'
    };

    const res = await firstValueFrom(
      this.http.post('/api/v1/attendance/exceptional-substitute', payload)
    );
    this.audioFeedback.playPresentTone();
    return res;
  }
}
