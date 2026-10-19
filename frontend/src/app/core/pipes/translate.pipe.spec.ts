import { TestBed } from '@angular/core/testing';
import { TranslatePipe } from './translate.pipe';
import { LocalizationService } from '../services/localization.service';

describe('TranslatePipe', () => {
  let pipe: TranslatePipe;
  let mockLocalizationService: jasmine.SpyObj<LocalizationService>;

  beforeEach(() => {
    mockLocalizationService = jasmine.createSpyObj<LocalizationService>('LocalizationService', ['translate']);

    TestBed.configureTestingModule({
      providers: [
        TranslatePipe,
        { provide: LocalizationService, useValue: mockLocalizationService }
      ]
    });

    pipe = TestBed.inject(TranslatePipe);
  });

  it('should delegate translation key and params to LocalizationService.translate()', () => {
    mockLocalizationService.translate.and.returnValue('مرحباً أحمد');

    const result = pipe.transform('common.welcome', { name: 'أحمد' });

    expect(mockLocalizationService.translate).toHaveBeenCalledWith('common.welcome', { name: 'أحمد' });
    expect(result).toBe('مرحباً أحمد');
  });

  it('should return empty string if service translates to empty string', () => {
    mockLocalizationService.translate.and.returnValue('');

    const result = pipe.transform('unknown.key');

    expect(mockLocalizationService.translate).toHaveBeenCalledWith('unknown.key', undefined);
    expect(result).toBe('');
  });
});
