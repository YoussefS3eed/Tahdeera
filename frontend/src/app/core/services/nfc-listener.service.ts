/**
 * Tahdeera Hardware NFC Keyboard-Wedge Listener Service
 * Milestone F1 Phase 5: Layout-independent global USB HID RFID/NFC reader listener
 */

import { Injectable, OnDestroy, signal, WritableSignal } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { VALIDATION_REGEX } from '../models/domain.models';

export interface NfcScanEvent {
  uid: string;           // Normalized uppercase hex string (e.g. '04A27C19')
  timestamp: number;     // Epoch millisecond when scan completed
}

// Primary mapping table: KeyboardEvent.code -> Hexadecimal ASCII
export const CODE_HEX_MAP: Record<string, string> = {
  KeyA: 'A', KeyB: 'B', KeyC: 'C', KeyD: 'D', KeyE: 'E', KeyF: 'F',
  Digit0: '0', Digit1: '1', Digit2: '2', Digit3: '3', Digit4: '4',
  Digit5: '5', Digit6: '6', Digit7: '7', Digit8: '8', Digit9: '9',
  Numpad0: '0', Numpad1: '1', Numpad2: '2', Numpad3: '3', Numpad4: '4',
  Numpad5: '5', Numpad6: '6', Numpad7: '7', Numpad8: '8', Numpad9: '9'
};

// Fallback mapping table: Arabic-Indic numerals -> ASCII Digits
export const ARABIC_INDIC_DIGIT_MAP: Record<string, string> = {
  '٠': '0', '١': '1', '٢': '2', '٣': '3', '٤': '4',
  '٥': '5', '٦': '6', '٧': '7', '٨': '8', '٩': '9'
};

@Injectable({
  providedIn: 'root'
})
export class NfcListenerService implements OnDestroy {
  // Reactive Signals
  readonly lastScannedUid: WritableSignal<string | null> = signal<string | null>(null);
  readonly lastScanTimestamp: WritableSignal<number | null> = signal<number | null>(null);
  readonly isListening: WritableSignal<boolean> = signal<boolean>(false);

  // Observable Event Stream
  private readonly scanSubject = new Subject<NfcScanEvent>();
  readonly scanEvents$: Observable<NfcScanEvent> = this.scanSubject.asObservable();

  // Timing thresholds & parameters
  readonly MAX_INTER_KEYSTROKE_MS = 50;
  readonly BUFFER_TIMEOUT_MS = 50;
  private duplicateThrottleMs = 1500;

  // Buffer state
  private keystrokeBuffer: string[] = [];
  private lastKeystrokeTime = 0;
  private bufferResetTimer: any = null;
  private lastProcessedUid: string | null = null;
  private lastProcessedTime = 0;

  // Bound handler reference
  private readonly keydownHandler = this.onKeyDown.bind(this);

  constructor() {
    this.startListening();
  }

  ngOnDestroy(): void {
    this.stopListening();
  }

  /**
   * Starts listening for global window keydown events in capture phase.
   */
  startListening(): void {
    if (this.isListening() || typeof window === 'undefined') {
      return;
    }
    window.addEventListener('keydown', this.keydownHandler, { capture: true });
    this.isListening.set(true);
  }

  /**
   * Stops listening and cleans up buffer and timers.
   */
  stopListening(): void {
    if (!this.isListening() || typeof window === 'undefined') {
      return;
    }
    window.removeEventListener('keydown', this.keydownHandler, { capture: true });
    if (this.bufferResetTimer) {
      clearTimeout(this.bufferResetTimer);
      this.bufferResetTimer = null;
    }
    this.keystrokeBuffer = [];
    this.isListening.set(false);
  }

  /**
   * Updates duplicate scan throttle window (in milliseconds).
   */
  setThrottleMs(ms: number): void {
    this.duplicateThrottleMs = ms;
  }

  /**
   * Simulates an NFC card scan programmatically (used in tests and test harness).
   */
  simulateScan(uid: string): boolean {
    const normalized = this.normalizeUid(uid);
    if (!this.isValidUid(normalized)) {
      return false;
    }
    const now = Date.now();
    this.lastScannedUid.set(normalized);
    this.lastScanTimestamp.set(now);
    this.scanSubject.next({ uid: normalized, timestamp: now });
    return true;
  }

  /**
   * Global keydown event listener.
   */
  private onKeyDown(event: KeyboardEvent): void {
    const currentTime = typeof performance !== 'undefined' ? performance.now() : Date.now();
    const delta = currentTime - this.lastKeystrokeTime;

    // Discard buffer if time between keystrokes exceeds automated scanner tolerance
    if (this.keystrokeBuffer.length > 0 && delta > this.MAX_INTER_KEYSTROKE_MS) {
      this.keystrokeBuffer = [];
    }

    if (this.bufferResetTimer) {
      clearTimeout(this.bufferResetTimer);
      this.bufferResetTimer = null;
    }

    // Check for scanner terminator (Enter / Return)
    const isTerminator = event.code === 'Enter' || event.key === 'Enter' || event.keyCode === 13;
    if (isTerminator) {
      if (this.keystrokeBuffer.length >= 8 && this.keystrokeBuffer.length <= 14) {
        this.processBuffer(event);
      }
      this.keystrokeBuffer = [];
      return;
    }

    // Extract character using layout-independent rules
    const char = this.extractCharacter(event);
    if (char) {
      this.keystrokeBuffer.push(char);
      this.lastKeystrokeTime = currentTime;
      this.bufferResetTimer = setTimeout(() => {
        this.keystrokeBuffer = [];
      }, this.BUFFER_TIMEOUT_MS);
    } else if (this.keystrokeBuffer.length > 0) {
      // Non-hex character received during active burst invalidates buffer
      this.keystrokeBuffer = [];
    }
  }

  /**
   * Extracts character using primary KeyboardEvent.code mapping, falling back to event.key.
   */
  extractCharacter(event: KeyboardEvent): string | null {
    // 1. Primary path: physical key code (immune to OS layout)
    if (event.code && CODE_HEX_MAP[event.code]) {
      return CODE_HEX_MAP[event.code];
    }

    // 2. Fallback path: event.key (used only when code is missing/unmapped)
    if (event.key) {
      if (ARABIC_INDIC_DIGIT_MAP[event.key]) {
        return ARABIC_INDIC_DIGIT_MAP[event.key];
      }
      if (/^[0-9A-Fa-f]$/.test(event.key)) {
        return event.key.toUpperCase();
      }
    }

    return null;
  }

  /**
   * Validates accumulated buffer, shields active inputs, throttles duplicates, and emits UID.
   */
  private processBuffer(event: KeyboardEvent): void {
    const raw = this.keystrokeBuffer.join('');
    const normalized = this.normalizeUid(raw);

    if (!this.isValidUid(normalized)) {
      return;
    }

    // Shield active input and prevent form submission
    event.preventDefault();
    event.stopPropagation();
    this.sanitizeActiveInput(raw);

    // Accidental double-tap throttling (per-card basis)
    const now = Date.now();
    if (this.lastProcessedUid === normalized && (now - this.lastProcessedTime) < this.duplicateThrottleMs) {
      return; // Duplicate scan suppressed
    }

    this.lastProcessedUid = normalized;
    this.lastProcessedTime = now;

    // Update state and notify subscribers
    this.lastScannedUid.set(normalized);
    this.lastScanTimestamp.set(now);
    this.scanSubject.next({ uid: normalized, timestamp: now });
  }

  /**
   * Normalizes raw card UID to uppercase alphanumeric format.
   */
  normalizeUid(raw: string): string {
    return raw.replace(/[\s:-]/g, '').toUpperCase();
  }

  /**
   * Verifies that the normalized UID matches the required format.
   */
  isValidUid(uid: string): boolean {
    return VALIDATION_REGEX.NFC_CARD_UID.test(uid);
  }

  /**
   * Removes injected scanner characters from active text inputs or textareas.
   */
  private sanitizeActiveInput(rawUid: string): void {
    if (typeof document === 'undefined') return;
    const activeEl = document.activeElement as HTMLElement | null;
    if (activeEl && (activeEl.tagName === 'INPUT' || activeEl.tagName === 'TEXTAREA')) {
      const input = activeEl as HTMLInputElement | HTMLTextAreaElement;
      if (typeof input.value === 'string' && input.value.length > 0) {
        if (input.value.endsWith(rawUid)) {
          input.value = input.value.slice(0, -rawUid.length);
        } else if (input.value.includes(rawUid)) {
          input.value = input.value.replace(rawUid, '');
        }
      }
    }
  }
}
