import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { LocalizationService } from './localization.service';
import { SupportedLanguage } from '../enums/domain.enums';

describe('LocalizationService', () => {
  let service: LocalizationService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        LocalizationService
      ]
    });

    service = TestBed.inject(LocalizationService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
    localStorage.clear();
    document.documentElement.removeAttribute('lang');
    document.documentElement.removeAttribute('dir');
  });

  it('should initialize with default Arabic and RTL direction', () => {
    expect(service.currentLang()).toBe(SupportedLanguage.Arabic);
    expect(service.direction()).toBe('rtl');
    expect(service.isRtl()).toBeTrue();
    expect(document.documentElement.getAttribute('lang')).toBe('ar');
    expect(document.documentElement.getAttribute('dir')).toBe('rtl');

    // Respond to initial http requests
    const reqAr = httpTesting.expectOne('/assets/i18n/ar.json');
    const reqEn = httpTesting.expectOne('/assets/i18n/en.json');
    reqAr.flush({});
    reqEn.flush({});
  });

  it('should serve from CRITICAL_FALLBACK_DICTIONARY while full dictionaries are loading', () => {
    expect(service.isLoaded()).toBeFalse();
    expect(service.translate('brand.name')).toBe('تحضيرة');
    expect(service.translate('common.loading')).toBe('جاري التحميل...');

    // Unknown key must return empty string, NEVER the raw key
    expect(service.translate('unknown.key')).toBe('');

    const reqAr = httpTesting.expectOne('/assets/i18n/ar.json');
    const reqEn = httpTesting.expectOne('/assets/i18n/en.json');
    reqAr.flush({});
    reqEn.flush({});
  });

  it('should switch to English, set LTR, and persist to localStorage', () => {
    const reqAr = httpTesting.expectOne('/assets/i18n/ar.json');
    const reqEn = httpTesting.expectOne('/assets/i18n/en.json');
    reqAr.flush({});
    reqEn.flush({});

    service.setLanguage(SupportedLanguage.English);

    expect(service.currentLang()).toBe(SupportedLanguage.English);
    expect(service.direction()).toBe('ltr');
    expect(service.isRtl()).toBeFalse();
    expect(document.documentElement.getAttribute('lang')).toBe('en');
    expect(document.documentElement.getAttribute('dir')).toBe('ltr');
    expect(localStorage.getItem('tahdeera_lang')).toBe('en');
  });

  it('should toggle between Arabic and English seamlessly', () => {
    const reqAr = httpTesting.expectOne('/assets/i18n/ar.json');
    const reqEn = httpTesting.expectOne('/assets/i18n/en.json');
    reqAr.flush({});
    reqEn.flush({});

    service.toggleLanguage();
    expect(service.currentLang()).toBe(SupportedLanguage.English);
    expect(service.direction()).toBe('ltr');

    service.toggleLanguage();
    expect(service.currentLang()).toBe(SupportedLanguage.Arabic);
    expect(service.direction()).toBe('rtl');
  });

  it('should resolve dot-notation translations and fall back to Arabic for missing English keys', () => {
    const reqAr = httpTesting.expectOne('/assets/i18n/ar.json');
    const reqEn = httpTesting.expectOne('/assets/i18n/en.json');

    reqAr.flush({
      common: {
        save: 'حفظ',
        onlyInArabic: 'فقط بالعربية'
      }
    });
    reqEn.flush({
      common: {
        save: 'Save'
      }
    });

    expect(service.isLoaded()).toBeTrue();

    // In Arabic
    expect(service.translate('common.save')).toBe('حفظ');

    // Switch to English
    service.setLanguage(SupportedLanguage.English);
    expect(service.translate('common.save')).toBe('Save');

    // Missing key in English should fall back to Arabic full dictionary
    expect(service.translate('common.onlyInArabic')).toBe('فقط بالعربية');

    // Completely unknown key must return empty string
    expect(service.translate('nonexistent.path')).toBe('');
  });

  it('should interpolate parameters and replace missing parameters with empty string while preserving 0 and false', () => {
    const reqAr = httpTesting.expectOne('/assets/i18n/ar.json');
    const reqEn = httpTesting.expectOne('/assets/i18n/en.json');

    reqAr.flush({
      msg: {
        welcome: 'مرحباً {{name}}!',
        count: 'العدد: {{count}}',
        status: 'نشط: {active}'
      }
    });
    reqEn.flush({});

    // Valid parameters
    expect(service.translate('msg.welcome', { name: 'علي' })).toBe('مرحباً علي!');
    expect(service.translate('msg.count', { count: 5 })).toBe('العدد: 5');

    // Preserving 0
    expect(service.translate('msg.count', { count: 0 })).toBe('العدد: 0');

    // Preserving false
    expect(service.translate('msg.status', { active: false })).toBe('نشط: false');

    // Missing parameter -> replace with empty string (""), never leak {{name}}
    expect(service.translate('msg.welcome', {})).toBe('مرحباً !');
    expect(service.translate('msg.welcome', { name: undefined })).toBe('مرحباً !');
    expect(service.translate('msg.welcome', { name: null })).toBe('مرحباً !');
  });

  it('should handle HTTP failure gracefully without throwing exceptions', () => {
    const reqAr = httpTesting.expectOne('/assets/i18n/ar.json');
    const reqEn = httpTesting.expectOne('/assets/i18n/en.json');

    reqAr.error(new ProgressEvent('Network error'));
    reqEn.error(new ProgressEvent('Network error'));

    expect(service.isLoaded()).toBeFalse();
    // Still serves from in-memory critical fallback dictionary
    expect(service.translate('brand.name')).toBe('تحضيرة');
    expect(service.translate('unknown.key')).toBe('');
  });
});
