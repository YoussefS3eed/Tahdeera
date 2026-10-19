import { Injectable, signal, WritableSignal } from '@angular/core';
import { ScanOutcome } from '../enums/domain.enums';

interface ActiveAudioVoice {
  oscillator: OscillatorNode;
  gainNode: GainNode;
  stopTime: number;
}

@Injectable({
  providedIn: 'root'
})
export class AudioFeedbackService {
  private readonly STORAGE_KEY = 'tahdeera_audio_muted';
  private readonly MAX_ACTIVE_VOICES = 4;
  private readonly MASTER_GAIN_CEILING = 0.4;

  private audioCtx?: AudioContext;
  private masterGainNode?: GainNode;
  private activeVoices: ActiveAudioVoice[] = [];

  readonly isAudioReady: WritableSignal<boolean>;
  readonly isMuted: WritableSignal<boolean>;

  constructor() {
    const initialMuted = this.safeGetStorage(this.STORAGE_KEY) === 'true';
    this.isMuted = signal<boolean>(initialMuted);
    this.isAudioReady = signal<boolean>(false);

    this.initAudioContext();
  }

  /**
   * Resumes AudioContext on authorized user gesture to comply with browser autoplay restrictions.
   */
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

  /**
   * Toggles the software mute state and persists to localStorage.
   */
  toggleMute(): void {
    const next = !this.isMuted();
    this.setMuted(next);
  }

  /**
   * Sets explicit software mute state and persists to localStorage.
   */
  setMuted(muted: boolean): void {
    this.isMuted.set(muted);
    this.safeSetStorage(this.STORAGE_KEY, String(muted));
  }

  /**
   * Plays the synthesized audio tone corresponding to the scan outcome.
   * Completely non-blocking and isolated in try/catch to never disrupt business workflows.
   */
  playOutcome(outcome: ScanOutcome | 'Present' | 'Late' | 'Duplicate' | 'Error' | 'Unregistered'): void {
    if (this.isMuted()) {
      return;
    }

    try {
      const ctx = this.getAudioContext();
      if (!ctx) return;

      // Update isAudioReady signal reflecting current context state
      this.isAudioReady.set(ctx.state === 'running');

      switch (outcome) {
        case ScanOutcome.Present:
        case 'Present':
          // Two-tone rising chime (880Hz + 1175Hz)
          this.triggerTone(880, 'sine', 0, 0.08, 0.25);
          this.triggerTone(1175, 'sine', 0.12, 0.12, 0.30);
          break;

        case ScanOutcome.Late:
        case 'Late':
          // Mellow warning triangle tone (587Hz)
          this.triggerTone(587, 'triangle', 0, 0.18, 0.35);
          break;

        case ScanOutcome.Duplicate:
        case 'Duplicate':
          // Double warning pulse (330Hz + 330Hz)
          this.triggerTone(330, 'sine', 0, 0.08, 0.25);
          this.triggerTone(330, 'sine', 0.12, 0.08, 0.25);
          break;

        case ScanOutcome.Unregistered:
        case ScanOutcome.Error:
        case 'Error':
        case 'Unregistered':
        default:
          // Low buzzing alert sawtooth tone (180Hz)
          this.triggerTone(180, 'sawtooth', 0, 0.25, 0.30);
          break;
      }
    } catch (err) {
      console.warn('AudioFeedbackService: Error during audio synthesis', err);
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

    // Polyphonic Voice Stealing Policy: Cap active voices at 4
    if (this.activeVoices.length >= this.MAX_ACTIVE_VOICES) {
      const oldest = this.activeVoices.shift()!;
      try {
        oldest.gainNode.gain.cancelScheduledValues(ctx.currentTime);
        oldest.gainNode.gain.setValueAtTime(oldest.gainNode.gain.value, ctx.currentTime);
        oldest.gainNode.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + 0.015);
        oldest.oscillator.stop(ctx.currentTime + 0.015);
      } catch {
        // Voice might have already finished
      }
    }

    const osc = ctx.createOscillator();
    const gain = ctx.createGain();

    osc.type = type;
    osc.frequency.setValueAtTime(freq, startTime);

    // Exponential ramp envelope to prevent acoustic pop/click
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
        // Ignore disconnect errors
      }
      const idx = this.activeVoices.indexOf(voice);
      if (idx !== -1) {
        this.activeVoices.splice(idx, 1);
      }
    };
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

  private getAudioContext(): AudioContext | undefined {
    if (!this.audioCtx) {
      this.initAudioContext();
    }
    return this.audioCtx;
  }

  private safeGetStorage(key: string): string | null {
    try {
      if (typeof window !== 'undefined' && window.localStorage) {
        return window.localStorage.getItem(key);
      }
    } catch {
      // Gracefully handle private browsing / quota exceptions
    }
    return null;
  }

  private safeSetStorage(key: string, value: string): void {
    try {
      if (typeof window !== 'undefined' && window.localStorage) {
        window.localStorage.setItem(key, value);
      }
    } catch {
      // Gracefully handle private browsing / quota exceptions
    }
  }
}
