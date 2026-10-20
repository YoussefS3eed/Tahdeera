/**
 * USB HID Keyboard Wedge Reader Listener Adapter
 * Captures rapid keystroke bursts (<50ms) from hardware barcode/NFC readers in capture phase,
 * suppressing default input propagation and emitting normalized UID strings.
 * Reference: specs/010-f3-students-nfc-attendance/spec.md
 */

import { Injectable, OnDestroy, signal, WritableSignal } from '@angular/core';
import { Observable, Subject } from 'rxjs';

export const CODE_HEX_MAP: Record<string, string> = {
  KeyA: 'A', KeyB: 'B', KeyC: 'C', KeyD: 'D', KeyE: 'E', KeyF: 'F',
  Digit0: '0', Digit1: '1', Digit2: '2', Digit3: '3', Digit4: '4',
  Digit5: '5', Digit6: '6', Digit7: '7', Digit8: '8', Digit9: '9',
  Numpad0: '0', Numpad1: '1', Numpad2: '2', Numpad3: '3', Numpad4: '4',
  Numpad5: '5', Numpad6: '6', Numpad7: '7', Numpad8: '8', Numpad9: '9'
};

export const ARABIC_INDIC_DIGIT_MAP: Record<string, string> = {
  '٠': '0', '١': '1', '٢': '2', '٣': '3', '٤': '4',
  '٥': '5', '٦': '6', '٧': '7', '٨': '8', '٩': '9'
};

@Injectable({
  providedIn: 'root'
})
export class NfcWedgeAdapterService implements OnDestroy {
  readonly lastScannedUid: WritableSignal<string | null> = signal<string | null>(null);
  readonly isListening: WritableSignal<boolean> = signal<boolean>(false);

  private readonly uidScannedSubject = new Subject<string>();
  readonly uidScanned$: Observable<string> = this.uidScannedSubject.asObservable();

  readonly MAX_INTER_KEYSTROKE_MS = 50;
  private keystrokeBuffer: string[] = [];
  private lastKeystrokeTime = 0;
  private bufferResetTimer: any = null;

  private readonly keydownHandler = this.onKeyDown.bind(this);

  constructor() {
    this.startListening();
  }

  ngOnDestroy(): void {
    this.stopListening();
  }

  startListening(): void {
    if (typeof window === 'undefined' || this.isListening()) return;
    window.addEventListener('keydown', this.keydownHandler, { capture: true, passive: false });
    this.isListening.set(true);
  }

  stopListening(): void {
    if (typeof window === 'undefined' || !this.isListening()) return;
    window.removeEventListener('keydown', this.keydownHandler, { capture: true });
    this.isListening.set(false);
    this.clearBuffer();
  }

  private onKeyDown(event: KeyboardEvent): void {
    const now = Date.now();
    const interval = now - this.lastKeystrokeTime;

    if (this.lastKeystrokeTime > 0 && interval > this.MAX_INTER_KEYSTROKE_MS) {
      this.clearBuffer();
    }

    this.lastKeystrokeTime = now;

    // Buffer timeout reset
    if (this.bufferResetTimer) {
      clearTimeout(this.bufferResetTimer);
    }
    this.bufferResetTimer = setTimeout(() => {
      this.clearBuffer();
    }, this.MAX_INTER_KEYSTROKE_MS * 2);

    // Enter key terminates scan
    if (event.key === 'Enter') {
      if (this.keystrokeBuffer.length >= 4) {
        event.preventDefault();
        event.stopPropagation();
        const raw = this.keystrokeBuffer.join('');
        const normalized = this.normalizeUid(raw);
        this.lastScannedUid.set(normalized);
        this.uidScannedSubject.next(normalized);
      }
      this.clearBuffer();
      return;
    }

    // Extract character
    const char = this.extractHexOrAscii(event);
    if (char) {
      this.keystrokeBuffer.push(char);
    }
  }

  private extractHexOrAscii(event: KeyboardEvent): string | null {
    if (event.code && CODE_HEX_MAP[event.code]) {
      return CODE_HEX_MAP[event.code];
    }
    const key = event.key;
    if (key && ARABIC_INDIC_DIGIT_MAP[key]) {
      return ARABIC_INDIC_DIGIT_MAP[key];
    }
    if (key && /^[0-9a-fA-F:-]$/.test(key)) {
      return key.toUpperCase();
    }
    return null;
  }

  private normalizeUid(raw: string): string {
    const cleaned = raw.replace(/[^0-9A-Fa-f]/g, '').toUpperCase();
    if (cleaned.length >= 8 && cleaned.length % 2 === 0) {
      const bytes: string[] = [];
      for (let i = 0; i < cleaned.length; i += 2) {
        bytes.push(cleaned.substring(i, i + 2));
      }
      return bytes.join(':');
    }
    return cleaned;
  }

  private clearBuffer(): void {
    this.keystrokeBuffer = [];
    this.lastKeystrokeTime = 0;
    if (this.bufferResetTimer) {
      clearTimeout(this.bufferResetTimer);
      this.bufferResetTimer = null;
    }
  }
}
