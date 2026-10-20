/**
 * Progressive Web NFC Adapter Service (Android Chrome)
 * Feature detection, NDEFReader lifecycle, and tag serial number extraction.
 * Reference: specs/010-f3-students-nfc-attendance/spec.md
 */

import { Injectable, OnDestroy, signal, WritableSignal } from '@angular/core';
import { Observable, Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class WebNfcAdapterService implements OnDestroy {
  readonly isSupported: WritableSignal<boolean>;
  readonly isScanning: WritableSignal<boolean> = signal<boolean>(false);
  readonly lastError: WritableSignal<string | null> = signal<string | null>(null);

  private readonly tagScannedSubject = new Subject<string>();
  readonly tagScanned$: Observable<string> = this.tagScannedSubject.asObservable();

  private ndefReader: any = null;
  private abortController: AbortController | null = null;

  constructor() {
    const supported = typeof window !== 'undefined' && 'NDEFReader' in window;
    this.isSupported = signal<boolean>(supported);
  }

  ngOnDestroy(): void {
    this.stopScanning();
  }

  async startScanning(): Promise<boolean> {
    if (!this.isSupported()) {
      this.lastError.set('Web NFC is not supported on this device/browser');
      return false;
    }

    try {
      this.abortController = new AbortController();
      const NDEFReaderClass = (window as any).NDEFReader;
      this.ndefReader = new NDEFReaderClass();

      await this.ndefReader.scan({ signal: this.abortController.signal });
      this.isScanning.set(true);
      this.lastError.set(null);

      this.ndefReader.onreading = (event: any) => {
        const serial = event.serialNumber;
        if (serial) {
          const normalized = serial.toUpperCase();
          this.tagScannedSubject.next(normalized);
        }
      };

      this.ndefReader.onreadingerror = () => {
        this.lastError.set('Cannot read NFC tag');
      };

      return true;
    } catch (err: any) {
      this.isScanning.set(false);
      this.lastError.set(err?.message || 'Permission denied or NFC unavailable');
      return false;
    }
  }

  stopScanning(): void {
    if (this.abortController) {
      try {
        this.abortController.abort();
      } catch {
        // Ignore abort error
      }
      this.abortController = null;
    }
    this.isScanning.set(false);
  }
}
