/**
 * Unit Tests for NfcListenerService
 * Milestone F1 Phase 5: Hardware NFC Keyboard-Wedge Listener
 */

import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { NfcListenerService, NfcScanEvent } from './nfc-listener.service';

describe('NfcListenerService', () => {
  let service: NfcListenerService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [NfcListenerService]
    });
    service = TestBed.inject(NfcListenerService);
  });

  afterEach(() => {
    service.stopListening();
  });

  it('should be created and start listening automatically', () => {
    expect(service).toBeTruthy();
    expect(service.isListening()).toBeTrue();
    expect(service.lastScannedUid()).toBeNull();
    expect(service.lastScanTimestamp()).toBeNull();
  });

  it('should capture a valid hexadecimal scan under English keyboard layout', fakeAsync(() => {
    let capturedEvent: NfcScanEvent | null = null;
    service.scanEvents$.subscribe(evt => capturedEvent = evt);

    // Keystrokes for 04A27C19 + Enter
    const keystrokes = [
      { code: 'Digit0', key: '0' },
      { code: 'Digit4', key: '4' },
      { code: 'KeyA', key: 'a' },
      { code: 'Digit2', key: '2' },
      { code: 'Digit7', key: '7' },
      { code: 'KeyC', key: 'c' },
      { code: 'Digit1', key: '1' },
      { code: 'Digit9', key: '9' },
      { code: 'Enter', key: 'Enter' }
    ];

    keystrokes.forEach(k => {
      window.dispatchEvent(new KeyboardEvent('keydown', {
        code: k.code,
        key: k.key,
        bubbles: true,
        cancelable: true
      }));
      tick(15);
    });

    expect(capturedEvent).not.toBeNull();
    expect((capturedEvent as any)?.uid).toBe('04A27C19');
    expect(service.lastScannedUid()).toBe('04A27C19');
    expect(service.lastScanTimestamp()).toBeGreaterThan(0);
  }));

  it('should capture a valid scan under Arabic OS keyboard layout using KeyboardEvent.code (Option A)', fakeAsync(() => {
    let capturedEvent: NfcScanEvent | null = null;
    service.scanEvents$.subscribe(evt => capturedEvent = evt);

    // In Arabic layout, KeyA emits 'ش' and KeyC emits 'ؤ'
    const keystrokes = [
      { code: 'Digit0', key: '0' },
      { code: 'Digit4', key: '4' },
      { code: 'KeyA', key: 'ش' }, // Arabic layout mapping
      { code: 'Digit2', key: '2' },
      { code: 'Digit7', key: '7' },
      { code: 'KeyC', key: 'ؤ' }, // Arabic layout mapping
      { code: 'Digit1', key: '1' },
      { code: 'Digit9', key: '9' },
      { code: 'Enter', key: 'Enter' }
    ];

    keystrokes.forEach(k => {
      window.dispatchEvent(new KeyboardEvent('keydown', {
        code: k.code,
        key: k.key,
        bubbles: true,
        cancelable: true
      }));
      tick(15);
    });

    expect(capturedEvent).not.toBeNull();
    expect((capturedEvent as any)?.uid).toBe('04A27C19');
    expect(service.lastScannedUid()).toBe('04A27C19');
  }));

  it('should support Numpad keys for digits', fakeAsync(() => {
    let capturedEvent: NfcScanEvent | null = null;
    service.scanEvents$.subscribe(evt => capturedEvent = evt);

    const keystrokes = [
      { code: 'Numpad1', key: '1' },
      { code: 'Numpad2', key: '2' },
      { code: 'KeyA', key: 'A' },
      { code: 'KeyB', key: 'B' },
      { code: 'Numpad3', key: '3' },
      { code: 'Numpad4', key: '4' },
      { code: 'KeyC', key: 'C' },
      { code: 'KeyD', key: 'D' },
      { code: 'Enter', key: 'Enter' }
    ];

    keystrokes.forEach(k => {
      window.dispatchEvent(new KeyboardEvent('keydown', {
        code: k.code,
        key: k.key,
        bubbles: true,
        cancelable: true
      }));
      tick(15);
    });

    expect((capturedEvent as any)?.uid).toBe('12AB34CD');
  }));

  it('should normalize Arabic-Indic digits when event.code is unavailable (Fallback mode)', fakeAsync(() => {
    let capturedEvent: NfcScanEvent | null = null;
    service.scanEvents$.subscribe(evt => capturedEvent = evt);

    // event.code is empty or undefined; event.key contains Arabic-Indic numerals ٠٤A٢٧C١٩
    const keystrokes = [
      { code: '', key: '٠' },
      { code: '', key: '٤' },
      { code: '', key: 'a' },
      { code: '', key: '٢' },
      { code: '', key: '٧' },
      { code: '', key: 'c' },
      { code: '', key: '١' },
      { code: '', key: '٩' },
      { code: 'Enter', key: 'Enter' }
    ];

    keystrokes.forEach(k => {
      window.dispatchEvent(new KeyboardEvent('keydown', {
        code: k.code,
        key: k.key,
        bubbles: true,
        cancelable: true
      }));
      tick(15);
    });

    expect(capturedEvent).not.toBeNull();
    expect((capturedEvent as any)?.uid).toBe('04A27C19');
  }));

  it('should discard buffer and reject human typing with > 50ms keystroke intervals', fakeAsync(() => {
    let emissions = 0;
    service.scanEvents$.subscribe(() => emissions++);

    // Keystrokes with 120ms intervals (human typing speed)
    const keystrokes = [
      { code: 'Digit0', key: '0' },
      { code: 'Digit4', key: '4' },
      { code: 'KeyA', key: 'a' },
      { code: 'Digit2', key: '2' },
      { code: 'Digit7', key: '7' },
      { code: 'KeyC', key: 'c' },
      { code: 'Digit1', key: '1' },
      { code: 'Digit9', key: '9' },
      { code: 'Enter', key: 'Enter' }
    ];

    keystrokes.forEach(k => {
      window.dispatchEvent(new KeyboardEvent('keydown', {
        code: k.code,
        key: k.key,
        bubbles: true,
        cancelable: true
      }));
      tick(120); // Exceeds 50ms limit
    });

    expect(emissions).toBe(0);
    expect(service.lastScannedUid()).toBeNull();
  }));

  it('should throttle accidental duplicate scans of the same card within 1500ms', fakeAsync(() => {
    let emissions = 0;
    service.scanEvents$.subscribe(() => emissions++);

    const fireScan = () => {
      const keystrokes = [
        { code: 'Digit0', key: '0' },
        { code: 'Digit4', key: '4' },
        { code: 'KeyA', key: 'a' },
        { code: 'Digit2', key: '2' },
        { code: 'Digit7', key: '7' },
        { code: 'KeyC', key: 'c' },
        { code: 'Digit1', key: '1' },
        { code: 'Digit9', key: '9' },
        { code: 'Enter', key: 'Enter' }
      ];
      keystrokes.forEach(k => {
        window.dispatchEvent(new KeyboardEvent('keydown', {
          code: k.code,
          key: k.key,
          bubbles: true,
          cancelable: true
        }));
        tick(10);
      });
    };

    // First scan
    fireScan();
    expect(emissions).toBe(1);

    // Second scan of identical card after 300ms (within 1500ms throttle)
    tick(300);
    fireScan();
    expect(emissions).toBe(1); // Suppressed

    // Third scan of identical card after 1600ms (after throttle window)
    tick(1600);
    fireScan();
    expect(emissions).toBe(2); // Accepted
  }));

  it('should immediately process a different card UID without throttling', fakeAsync(() => {
    const emittedUids: string[] = [];
    service.scanEvents$.subscribe(e => emittedUids.push(e.uid));

    const fireCustomScan = (uidCodes: string[]) => {
      uidCodes.forEach(code => {
        window.dispatchEvent(new KeyboardEvent('keydown', {
          code,
          key: code.slice(-1),
          bubbles: true,
          cancelable: true
        }));
        tick(10);
      });
      window.dispatchEvent(new KeyboardEvent('keydown', { code: 'Enter', key: 'Enter', bubbles: true }));
      tick(10);
    };

    // Card 1: 04A27C19
    fireCustomScan(['Digit0', 'Digit4', 'KeyA', 'Digit2', 'Digit7', 'KeyC', 'Digit1', 'Digit9']);
    expect(emittedUids).toEqual(['04A27C19']);

    // Card 2 immediately at +200ms: A1B2C3D4
    tick(200);
    fireCustomScan(['KeyA', 'Digit1', 'KeyB', 'Digit2', 'KeyC', 'Digit3', 'KeyD', 'Digit4']);
    expect(emittedUids).toEqual(['04A27C19', 'A1B2C3D4']);
  }));

  it('should support simulateScan for testing and test harness', () => {
    let captured: NfcScanEvent | null = null;
    service.scanEvents$.subscribe(evt => captured = evt);

    const result = service.simulateScan('04:A2:7C:19');
    expect(result).toBeTrue();
    expect((captured as any)?.uid).toBe('04A27C19');
    expect(service.lastScannedUid()).toBe('04A27C19');

    // Invalid format returns false
    expect(service.simulateScan('INVALID-UID-1234567890')).toBeFalse();
  });

  it('should cleanly stop and start listening without leaks', () => {
    service.stopListening();
    expect(service.isListening()).toBeFalse();

    // Keydown when not listening should not populate buffer
    window.dispatchEvent(new KeyboardEvent('keydown', { code: 'KeyA', key: 'a' }));
    expect(service.lastScannedUid()).toBeNull();

    service.startListening();
    expect(service.isListening()).toBeTrue();
  });
});
