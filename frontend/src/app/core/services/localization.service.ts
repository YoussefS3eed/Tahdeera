import { Injectable, signal, computed, WritableSignal, Signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SupportedLanguage } from '../enums/domain.enums';

export const CRITICAL_FALLBACK_DICTIONARY: Record<SupportedLanguage, Record<string, string>> = {
  [SupportedLanguage.Arabic]: {
    'brand.name': 'تحضيرة',
    'brand.tagline': 'منظومة الحضور الذكي وإدارة السناتر التعليمية',
    'common.loading': 'جاري التحميل...',
    'ADMIN_SHELL.TITLE': 'لوحة تحكم إدارة المنصة',
    'ADMIN_SHELL.SUPER_ADMIN_BADGE': 'مدير النظام العام',
    'ADMIN_SHELL.TAB_OVERVIEW': 'نظرة عامة',
    'ADMIN_SHELL.TAB_ACCOUNTS': 'حسابات السناتر والمدرسين',
    'ADMIN_SHELL.TAB_SUBSCRIPTIONS': 'الاشتراكات والتجديدات',
    'ADMIN_SHELL.TAB_PACKAGES': 'الباقات والصلاحيات',
    'ADMIN_SHELL.TAB_REVENUE': 'إيرادات المنصة',
    'ADMIN_SHELL.TAB_WHATSAPP': 'إدارة الواتساب المركزية',
    'ADMIN_SHELL.TAB_SETTINGS': 'إعدادات المنصة العامة',
    'ADMIN_SHELL.TAB_AUDIT': 'سجل الرقابة والأمان',
    'ADMIN_SHELL.IMPERSONATING_NOTICE': 'أنت تتصفح حالياً بصلاحية مدير سنتر:',
    'ADMIN_SHELL.END_IMPERSONATION': 'إنهاء التصفح والعودة للإدارة',
    'ADMIN_OVERVIEW.PAGE_TITLE': 'نظرة عامة على المنصة',
    'ADMIN_OVERVIEW.SUBTITLE': 'إدارة حسابات المدرسين والسناتر والاشتراكات ومراقبة الأداء العام',
    'ADMIN_OVERVIEW.KPI_REGISTERED': 'إجمالي السناتر والمدرسين',
    'ADMIN_OVERVIEW.KPI_STUDENTS_USAGE': 'إجمالي الطلاب المسجلين',
    'ADMIN_OVERVIEW.KPI_COLLECTED_REVENUE': 'الإيراد الشهري للمنصة',
    'ADMIN_OVERVIEW.KPI_EXPIRING_SOON': 'اشتراكات توشك على الانتهاء',
    'FORBIDDEN_PAGE.TITLE': 'عفواً، هذه الصفحة مخصصة لمدير النظام العام للمنصة فقط',
    'FORBIDDEN_PAGE.SUBTEXT': 'حسابك الحالي لا يمتلك الصلاحيات الإدارية المركزية للوصول إلى لوحة تحكم المنصة.',
    'FORBIDDEN_PAGE.RETURN_BTN': 'العودة للوحة تحكم السنتر'
  },
  [SupportedLanguage.English]: {
    'brand.name': 'Tahdeera',
    'brand.tagline': 'Smart Attendance & Center Management System',
    'common.loading': 'Loading...',
    'ADMIN_SHELL.TITLE': 'Platform Administration Portal',
    'ADMIN_SHELL.SUPER_ADMIN_BADGE': 'Super Admin',
    'ADMIN_SHELL.TAB_OVERVIEW': 'Overview',
    'ADMIN_SHELL.TAB_ACCOUNTS': 'Centers & Teachers',
    'ADMIN_SHELL.TAB_SUBSCRIPTIONS': 'Subscriptions & Renewals',
    'ADMIN_SHELL.TAB_PACKAGES': 'Packages & Features',
    'ADMIN_SHELL.TAB_REVENUE': 'Platform Revenue',
    'ADMIN_SHELL.TAB_WHATSAPP': 'Central WhatsApp',
    'ADMIN_SHELL.TAB_SETTINGS': 'Platform Settings',
    'ADMIN_SHELL.TAB_AUDIT': 'Audit Log',
    'ADMIN_SHELL.IMPERSONATING_NOTICE': 'You are currently browsing as Center Admin:',
    'ADMIN_SHELL.END_IMPERSONATION': 'End Session & Return to Admin',
    'ADMIN_OVERVIEW.PAGE_TITLE': 'Platform Overview',
    'ADMIN_OVERVIEW.SUBTITLE': 'Manage teacher and center accounts, subscriptions, and monitor platform health',
    'ADMIN_OVERVIEW.KPI_REGISTERED': 'Registered Accounts',
    'ADMIN_OVERVIEW.KPI_STUDENTS_USAGE': 'Aggregate Student Usage',
    'ADMIN_OVERVIEW.KPI_COLLECTED_REVENUE': 'Total Collected Revenue',
    'ADMIN_OVERVIEW.KPI_EXPIRING_SOON': 'Expiring in 7 Days',
    'FORBIDDEN_PAGE.TITLE': 'Access Restricted to Platform Super Admin Only',
    'FORBIDDEN_PAGE.SUBTEXT': 'You do not have permission to access the central platform administration portal.',
    'FORBIDDEN_PAGE.RETURN_BTN': 'Back to Dashboard'
  }
};

@Injectable({
  providedIn: 'root'
})
export class LocalizationService {
  private readonly STORAGE_KEY = 'tahdeera_lang';
  private dictionaries: Partial<Record<SupportedLanguage, any>> = {};

  readonly currentLang: WritableSignal<SupportedLanguage>;
  readonly direction: Signal<'rtl' | 'ltr'>;
  readonly isRtl: Signal<boolean>;
  readonly isLoaded: WritableSignal<boolean>;

  constructor(private http: HttpClient) {
    const initialLang = this.getInitialLanguage();
    this.currentLang = signal<SupportedLanguage>(initialLang);

    this.direction = computed<'rtl' | 'ltr'>(() =>
      this.currentLang() === SupportedLanguage.Arabic ? 'rtl' : 'ltr'
    );

    this.isRtl = computed<boolean>(() => this.direction() === 'rtl');
    this.isLoaded = signal<boolean>(false);

    // Synchronously enforce Zero-FOUC on documentElement
    this.applyLanguageToDom(this.currentLang(), this.direction());

    // Eagerly and asynchronously load dictionaries
    this.loadDictionaries();
  }

  /**
   * Sets active language, updates DOM attributes, and persists to localStorage.
   */
  setLanguage(lang: SupportedLanguage): void {
    this.currentLang.set(lang);
    this.safeSetStorage(this.STORAGE_KEY, lang);
    this.applyLanguageToDom(lang, this.direction());
  }

  /**
   * Toggles between Arabic and English.
   */
  toggleLanguage(): void {
    const nextLang =
      this.currentLang() === SupportedLanguage.Arabic
        ? SupportedLanguage.English
        : SupportedLanguage.Arabic;
    this.setLanguage(nextLang);
  }

  /**
   * Resolves a dot-notation key, applies Arabic fallback for missing English keys,
   * checks critical fallback dictionary if still loading, replaces parameters,
   * and returns empty string ("") if unresolved. NEVER returns raw keys.
   */
  translate(key: string, params?: Record<string, any>): string {
    if (!key) return '';

    const lang = this.currentLang();
    let template: string | undefined;

    // 1. Resolve in active dictionary
    if (this.dictionaries[lang]) {
      template = this.resolveDotNotation(this.dictionaries[lang], key);
    }

    // 2. Fall back to Arabic full dictionary if missing in English
    if (template === undefined && lang === SupportedLanguage.English && this.dictionaries[SupportedLanguage.Arabic]) {
      template = this.resolveDotNotation(this.dictionaries[SupportedLanguage.Arabic], key);
    }

    // 3. Fall back to in-memory Critical Fallback Dictionary
    if (template === undefined) {
      template = CRITICAL_FALLBACK_DICTIONARY[lang]?.[key] ??
                 CRITICAL_FALLBACK_DICTIONARY[SupportedLanguage.Arabic]?.[key];
    }

    // 4. If still unresolved, return empty string (NEVER expose raw keys in UI)
    if (template === undefined) {
      return '';
    }

    // 5. Interpolate placeholders
    return this.interpolate(template, params);
  }

  private loadDictionaries(): void {
    let arLoaded = false;
    let enLoaded = false;

    const checkComplete = () => {
      if (arLoaded && enLoaded) {
        this.isLoaded.set(true);
      }
    };

    this.http.get<any>('/assets/i18n/ar.json').subscribe({
      next: (data) => {
        this.dictionaries[SupportedLanguage.Arabic] = data;
        arLoaded = true;
        checkComplete();
      },
      error: (err) => {
        console.warn('LocalizationService: Failed to load ar.json, serving fallback dictionary', err);
      }
    });

    this.http.get<any>('/assets/i18n/en.json').subscribe({
      next: (data) => {
        this.dictionaries[SupportedLanguage.English] = data;
        enLoaded = true;
        checkComplete();
      },
      error: (err) => {
        console.warn('LocalizationService: Failed to load en.json, serving fallback dictionary', err);
      }
    });
  }

  private resolveDotNotation(dict: any, path: string): string | undefined {
    if (!dict || typeof dict !== 'object') return undefined;
    const parts = path.split('.');
    let current = dict;
    for (const part of parts) {
      if (current === null || current === undefined || typeof current !== 'object') {
        return undefined;
      }
      current = current[part];
    }
    return typeof current === 'string' ? current : undefined;
  }

  private interpolate(template: string, params?: Record<string, any>): string {
    if (!template) return '';
    // Matches both {{param}} and {param}
    return template.replace(
      /\{\{\s*([a-zA-Z0-9_]+)\s*\}\}|\{\s*([a-zA-Z0-9_]+)\s*\}/g,
      (_, tokenDouble, tokenSingle) => {
        const token = tokenDouble || tokenSingle;
        if (!params || params[token] === undefined || params[token] === null) {
          return ''; // Replace missing/null/undefined parameter with empty string
        }
        return String(params[token]); // Strictly preserves 0, false, and non-empty strings
      }
    );
  }

  private applyLanguageToDom(lang: SupportedLanguage, dir: 'rtl' | 'ltr'): void {
    if (typeof document !== 'undefined' && document.documentElement) {
      document.documentElement.setAttribute('lang', lang);
      document.documentElement.setAttribute('dir', dir);
    }
  }

  private getInitialLanguage(): SupportedLanguage {
    const stored = this.safeGetStorage(this.STORAGE_KEY);
    if (stored === SupportedLanguage.Arabic || stored === SupportedLanguage.English) {
      return stored as SupportedLanguage;
    }
    return SupportedLanguage.Arabic;
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
