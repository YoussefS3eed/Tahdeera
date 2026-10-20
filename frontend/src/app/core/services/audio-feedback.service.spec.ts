import { TestBed } from '@angular/core/testing';
import { AudioFeedbackService } from './audio-feedback.service';
import { ScanOutcome } from '../enums/domain.enums';

describe('AudioFeedbackService', () => {
  let service: AudioFeedbackService;
  let mockAudioContext: any;
  let mockMasterGain: any;
  let createdOscillators: any[];
  let createdGains: any[];
  let createdFilters: any[];

  beforeEach(() => {
    localStorage.clear();
    createdOscillators = [];
    createdGains = [];
    createdFilters = [];

    mockMasterGain = {
      gain: { value: 0 },
      connect: jasmine.createSpy('connect')
    };

    mockAudioContext = {
      state: 'suspended',
      currentTime: 10,
      destination: {},
      resume: jasmine.createSpy('resume').and.callFake(async () => {
        mockAudioContext.state = 'running';
      }),
      createGain: jasmine.createSpy('createGain').and.callFake(() => {
        const gain = {
          gain: {
            value: 1,
            setValueAtTime: jasmine.createSpy('setValueAtTime'),
            exponentialRampToValueAtTime: jasmine.createSpy('exponentialRampToValueAtTime'),
            cancelScheduledValues: jasmine.createSpy('cancelScheduledValues')
          },
          connect: jasmine.createSpy('connect'),
          disconnect: jasmine.createSpy('disconnect')
        };
        createdGains.push(gain);
        return gain;
      }),
      createOscillator: jasmine.createSpy('createOscillator').and.callFake(() => {
        const osc = {
          type: 'sine',
          frequency: {
            setValueAtTime: jasmine.createSpy('setValueAtTime')
          },
          connect: jasmine.createSpy('connect'),
          disconnect: jasmine.createSpy('disconnect'),
          start: jasmine.createSpy('start'),
          stop: jasmine.createSpy('stop'),
          onended: null as any
        };
        createdOscillators.push(osc);
        return osc;
      }),
      createBiquadFilter: jasmine.createSpy('createBiquadFilter').and.callFake(() => {
        const filter = {
          type: 'lowpass',
          frequency: {
            setValueAtTime: jasmine.createSpy('setValueAtTime'),
            value: 400
          },
          connect: jasmine.createSpy('connect'),
          disconnect: jasmine.createSpy('disconnect')
        };
        createdFilters.push(filter);
        return filter;
      })
    };

    // Global mock
    (window as any).AudioContext = jasmine.createSpy('AudioContext').and.returnValue(mockAudioContext);

    TestBed.configureTestingModule({
      providers: [AudioFeedbackService]
    });

    service = TestBed.inject(AudioFeedbackService);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should initialize with master gain capped at 0.4', () => {
    expect(service.isAudioReady()).toBeFalse();
    expect(service.isMuted()).toBeFalse();
    expect(createdGains.length).toBeGreaterThanOrEqual(1);
    expect(createdGains[0].gain.value).toBe(0.4);
    expect(createdGains[0].connect).toHaveBeenCalledWith(mockAudioContext.destination);
  });

  it('should unlock audio context on resumeAudio()', async () => {
    expect(service.isAudioReady()).toBeFalse();

    const result = await service.resumeAudio();

    expect(result).toBeTrue();
    expect(mockAudioContext.resume).toHaveBeenCalled();
    expect(service.isAudioReady()).toBeTrue();
  });

  it('should toggle and set software mute state with localStorage persistence', () => {
    expect(service.isMuted()).toBeFalse();

    service.toggleMute();
    expect(service.isMuted()).toBeTrue();

    const stored = localStorage.getItem('tahdeera_station_audio_muted') || localStorage.getItem('tahdeera_audio_muted');
    expect(stored).toBe('true');

    service.setMuted(false);
    expect(service.isMuted()).toBeFalse();
  });

  it('should suppress audio playback when muted', () => {
    service.setMuted(true);
    const initialOscCount = createdOscillators.length;

    service.playOutcome(ScanOutcome.Present);

    expect(createdOscillators.length).toBe(initialOscCount);
  });

  it('should synthesize two tones for ScanOutcome.Present (880Hz and 1760Hz)', () => {
    service.playOutcome(ScanOutcome.Present);

    expect(createdOscillators.length).toBe(2);
    expect(createdOscillators[0].type).toBe('sine');
    expect(createdOscillators[0].frequency.setValueAtTime).toHaveBeenCalledWith(880, 10);
    expect(createdOscillators[1].type).toBe('sine');
    expect(createdOscillators[1].frequency.setValueAtTime).toHaveBeenCalledWith(1760, 10.13);
  });

  it('should synthesize a triangle tone for ScanOutcome.Late (587.33Hz)', () => {
    service.playOutcome(ScanOutcome.Late);

    expect(createdOscillators.length).toBe(1);
    expect(createdOscillators[0].type).toBe('triangle');
    expect(createdOscillators[0].frequency.setValueAtTime).toHaveBeenCalledWith(587.33, 10);
  });

  it('should synthesize two 220Hz pulses for ScanOutcome.Duplicate', () => {
    service.playOutcome(ScanOutcome.Duplicate);

    expect(createdOscillators.length).toBe(2);
    expect(createdOscillators[0].frequency.setValueAtTime).toHaveBeenCalledWith(220, 10);
    expect(createdOscillators[1].frequency.setValueAtTime).toHaveBeenCalledWith(220, 10.16);
  });

  it('should synthesize a sawtooth buzzer for ScanOutcome.Error (110Hz)', () => {
    service.playOutcome(ScanOutcome.Error);

    expect(createdOscillators.length).toBe(1);
    expect(createdOscillators[0].type).toBe('sawtooth');
    expect(createdOscillators[0].frequency.setValueAtTime).toHaveBeenCalledWith(110, 10);
  });

  it('should enforce 4-voice limit and steal oldest voice when capacity is reached', () => {
    service.playOutcome(ScanOutcome.Present); // 2 voices
    service.playOutcome(ScanOutcome.Duplicate); // 2 voices (now 4)
    expect(createdOscillators.length).toBe(4);

    const firstGain = createdGains[1]; // first tone gain (index 0 is master gain)

    service.playOutcome(ScanOutcome.Late);
    expect(firstGain.gain.exponentialRampToValueAtTime).toHaveBeenCalledWith(0.0001, 10.015);
  });
});
