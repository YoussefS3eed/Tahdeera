import { Injectable, OnDestroy, signal, WritableSignal } from '@angular/core';
import { ScanOutcome } from '../enums/domain.enums';
import { ScanOutcomeStatus } from '../models/academic.models';

interface ActiveAudioVoice {
  oscillator: OscillatorNode;
  gainNode: GainNode;
  stopTime: number;
}

@Injectable({
  providedIn: 'root'
})
export class AudioFeedbackService implements OnDestroy {
  private readonly STORAGE_KEY = 'tahdeera_station_audio_muted';
  private readonly MAX_ACTIVE_VOICES = 4;
  private readonly MASTER_GAIN_CEILING = 0.4;

  private audioCtx?: AudioContext;
  private masterGainNode?: GainNode;
  private activeVoices: ActiveAudioVoice[] = [];
  private unlockListenerAttached = false;
  private readonly boundGestureUnlock = () => this.handleFirstUserGesture();

  readonly isAudioReady: WritableSignal<boolean>;
  readonly isMuted: WritableSignal<boolean>;

  constructor() {
    const initialMuted = this.safeGetStorage(this.STORAGE_KEY) === 'true';
    this.isMuted = signal<boolean>(initialMuted);
    this.isAudioReady = signal<boolean>(false);

    this.initAudioContext();
    this.attachUserGestureUnlock();
  }

  ngOnDestroy(): void {
    this.detachUserGestureUnlock();
    if (this.audioCtx && this.audioCtx.state !== 'closed') {
      try {
        this.audioCtx.close();
      } catch {
        // Ignore close error on destruction
      }
    }
  }

  async resumeAudio(): Promise<boolean> {
    try {
      const ctx = this.getAudioContext();
      if (!ctx) return false;

      if (ctx.state === 'suspended') {
        await ctx.resume();
      }

      const ready = ctx.state === 'running';
      this.isAudioReady.set(ready);
      return ready;
    } catch (err) {
      console.warn('AudioFeedbackService: Failed to resume AudioContext', err);
      return false;
    }
  }

  toggleMute(): void {
    const next = !this.isMuted();
    this.setMuted(next);
  }

  setMuted(muted: boolean): void {
    this.isMuted.set(muted);
    this.safeSetStorage(this.STORAGE_KEY, String(muted));
  }

  /**
   * Present: Dual high chime (880 Hz + 1760 Hz sine wave)
   */
  playPresentTone(): void {
    if (this.isMuted()) return;
    try {
      this.triggerTone(880, 'sine', 0, 0.10, 0.30);
      this.triggerTone(1760, 'sine', 0.13, 0.12, 0.35);
    } catch (err) {
      console.warn('AudioFeedbackService: Failed to play Present tone', err);
    }
  }

  /**
   * Late: Single warning chime (587.33 Hz triangle wave)
   */
  playLateTone(): void {
    if (this.isMuted()) return;
    try {
      this.triggerTone(587.33, 'triangle', 0, 0.16, 0.35);
    } catch (err) {
      console.warn('AudioFeedbackService: Failed to play Late tone', err);
    }
  }

  /**
   * Duplicate: Low double tone (220 Hz sine wave)
   */
  playDuplicateTone(): void {
    if (this.isMuted()) return;
    try {
      this.triggerTone(220, 'sine', 0, 0.12, 0.28);
      this.triggerTone(220, 'sine', 0.16, 0.12, 0.28);
    } catch (err) {
      console.warn('AudioFeedbackService: Failed to play Duplicate tone', err);
    }
  }

  /**
   * Error / Revoked / Wrong Group: Low buzz (110 Hz sawtooth wave with 400Hz low-pass filter)
   */
  playErrorBuzz(): void {
    if (this.isMuted()) return;
    try {
      this.triggerToneWithFilter(110, 'sawtooth', 0, 0.32, 0.30, 400);
    } catch (err) {
      console.warn('AudioFeedbackService: Failed to play Error buzz', err);
    }
  }

  /**
   * Dispatches sound corresponding to outcome
   */
  playOutcome(outcome: ScanOutcome | ScanOutcomeStatus | string): void {
    switch (outcome) {
      case ScanOutcome.Present:
      case 'Present':
        this.playPresentTone();
        break;
      case ScanOutcome.Late:
      case 'Late':
        this.playLateTone();
        break;
      case ScanOutcome.Duplicate:
      case 'Duplicate':
        this.playDuplicateTone();
        break;
      case ScanOutcome.Unregistered:
      case 'Unregistered':
      case 'WrongGroup':
      case 'Revoked':
      case 'SessionClosed':
      case 'Error':
      default:
        this.playErrorBuzz();
        break;
    }
  }

  private triggerTone(
    freq: number,
    type: OscillatorType,
    offsetSec: number,
    durationSec: number,
    peakGainVal: number
  ): void {
    const ctx = this.getAudioContext();
    if (!ctx || !this.masterGainNode) return;

    const startTime = ctx.currentTime + offsetSec;
    const stopTime = startTime + durationSec;

    this.pruneVoices(ctx);

    const osc = ctx.createOscillator();
    const gain = ctx.createGain();

    osc.type = type;
    osc.frequency.setValueAtTime(freq, startTime);

    gain.gain.setValueAtTime(0.0001, startTime);
    gain.gain.exponentialRampToValueAtTime(peakGainVal, startTime + 0.015);
    gain.gain.exponentialRampToValueAtTime(0.0001, stopTime);

    osc.connect(gain);
    gain.connect(this.masterGainNode);

    osc.start(startTime);
    osc.stop(stopTime);

    const voice: ActiveAudioVoice = { oscillator: osc, gainNode: gain, stopTime };
    this.activeVoices.push(voice);

    osc.onended = () => {
      try {
        osc.disconnect();
        gain.disconnect();
      } catch {
        // Voice cleanup
      }
      const idx = this.activeVoices.indexOf(voice);
      if (idx !== -1) {
        this.activeVoices.splice(idx, 1);
      }
    };
  }

  private triggerToneWithFilter(
    freq: number,
    type: OscillatorType,
    offsetSec: number,
    durationSec: number,
    peakGainVal: number,
    filterCutoffHz: number
  ): void {
    const ctx = this.getAudioContext();
    if (!ctx || !this.masterGainNode) return;

    const startTime = ctx.currentTime + offsetSec;
    const stopTime = startTime + durationSec;

    this.pruneVoices(ctx);

    const osc = ctx.createOscillator();
    const filter = ctx.createBiquadFilter();
    const gain = ctx.createGain();

    osc.type = type;
    osc.frequency.setValueAtTime(freq, startTime);

    filter.type = 'lowpass';
    filter.frequency.setValueAtTime(filterCutoffHz, startTime);

    gain.gain.setValueAtTime(0.0001, startTime);
    gain.gain.exponentialRampToValueAtTime(peakGainVal, startTime + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, stopTime);

    osc.connect(filter);
    filter.connect(gain);
    gain.connect(this.masterGainNode);

    osc.start(startTime);
    osc.stop(stopTime);

    const voice: ActiveAudioVoice = { oscillator: osc, gainNode: gain, stopTime };
    this.activeVoices.push(voice);

    osc.onended = () => {
      try {
        osc.disconnect();
        filter.disconnect();
        gain.disconnect();
      } catch {
        // Voice cleanup
      }
      const idx = this.activeVoices.indexOf(voice);
      if (idx !== -1) {
        this.activeVoices.splice(idx, 1);
      }
    };
  }

  private pruneVoices(ctx: AudioContext): void {
    if (this.activeVoices.length >= this.MAX_ACTIVE_VOICES) {
      const oldest = this.activeVoices.shift()!;
      try {
        oldest.gainNode.gain.cancelScheduledValues(ctx.currentTime);
        oldest.gainNode.gain.setValueAtTime(oldest.gainNode.gain.value, ctx.currentTime);
        oldest.gainNode.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + 0.015);
        oldest.oscillator.stop(ctx.currentTime + 0.015);
      } catch {
        // Voice already terminated
      }
    }
  }

  private initAudioContext(): void {
    try {
      if (typeof window !== 'undefined') {
        const AudioCtx = window.AudioContext || (window as any).webkitAudioContext;
        if (AudioCtx) {
          this.audioCtx = new AudioCtx();
          this.masterGainNode = this.audioCtx.createGain();
          this.masterGainNode.gain.value = this.MASTER_GAIN_CEILING;
          this.masterGainNode.connect(this.audioCtx.destination);
          this.isAudioReady.set(this.audioCtx.state === 'running');
        }
      }
    } catch (err) {
      console.warn('AudioFeedbackService: Web Audio API not supported or initialization failed', err);
    }
  }

  private attachUserGestureUnlock(): void {
    if (typeof window === 'undefined' || typeof document === 'undefined' || this.unlockListenerAttached) {
      return;
    }
    this.unlockListenerAttached = true;
    const events = ['click', 'keydown', 'touchstart'];
    events.forEach(evt => {
      document.addEventListener(evt, this.boundGestureUnlock, { capture: true, once: true, passive: true });
    });
  }

  private detachUserGestureUnlock(): void {
    if (typeof document === 'undefined' || !this.unlockListenerAttached) return;
    this.unlockListenerAttached = false;
    const events = ['click', 'keydown', 'touchstart'];
    events.forEach(evt => {
      document.removeEventListener(evt, this.boundGestureUnlock, { capture: true });
    });
  }

  private handleFirstUserGesture(): void {
    this.detachUserGestureUnlock();
    this.resumeAudio();
  }

  private getAudioContext(): AudioContext | undefined {
    if (!this.audioCtx) {
      this.initAudioContext();
    }
    return this.audioCtx;
  }

  private safeGetStorage(key: string): string | null {
    try {
      if (typeof localStorage !== 'undefined') {
        return localStorage.getItem(key);
      }
    } catch {
      // LocalStorage access restricted
    }
    return null;
  }

  private safeSetStorage(key: string, value: string): void {
    try {
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(key, value);
      }
    } catch {
      // LocalStorage write restricted
    }
  }
}
