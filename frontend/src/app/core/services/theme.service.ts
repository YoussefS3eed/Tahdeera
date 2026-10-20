import { Injectable, signal, computed, WritableSignal, Signal } from '@angular/core';
import { ThemeMode } from '../enums/domain.enums';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly STORAGE_KEY = 'tahdeera_theme';
  private mediaQuery?: MediaQueryList;
  private mediaQueryListener?: () => void;
  private readonly systemTheme: WritableSignal<'light' | 'dark'>;

  readonly themeMode: WritableSignal<ThemeMode>;
  readonly effectiveTheme: Signal<'light' | 'dark'>;
  readonly isDarkMode: Signal<boolean>;

  constructor() {
    const initialMode = this.getInitialThemeMode();
    this.systemTheme = signal<'light' | 'dark'>(this.resolveSystemTheme());
    this.themeMode = signal<ThemeMode>(initialMode);

    this.effectiveTheme = computed<'light' | 'dark'>(() => {
      const mode = this.themeMode();
      if (mode === ThemeMode.System) {
        return this.systemTheme();
      }
      return mode === ThemeMode.Dark ? 'dark' : 'light';
    });

    this.isDarkMode = computed<boolean>(() => this.effectiveTheme() === 'dark');

    // Synchronously apply initial theme to DOM to maintain Zero-FOUC
    this.applyThemeToDom(this.effectiveTheme());

    // Register OS media query listener for live theme changes in 'system' mode
    this.setupMediaQueryListener();
  }

  /**
   * Sets the theme mode, applies it to DOM, and persists to localStorage.
   */
  setTheme(mode: ThemeMode): void {
    if (mode === ThemeMode.System) {
      this.systemTheme.set(this.resolveSystemTheme());
    }
    this.themeMode.set(mode);
    this.safeSetStorage(this.STORAGE_KEY, mode);
    this.applyThemeToDom(this.effectiveTheme());
  }

  /**
   * Toggles the current theme.
   * If in 'system' mode, flips to the opposite of the current effective visual appearance,
   * exits 'system' mode, and persists the explicit user override.
   */
  toggleTheme(): void {
    if (this.themeMode() === ThemeMode.System) {
      const target = this.effectiveTheme() === 'dark' ? ThemeMode.Light : ThemeMode.Dark;
      this.setTheme(target);
    } else {
      const target = this.themeMode() === ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
      this.setTheme(target);
    }
  }

  private resolveSystemTheme(): 'light' | 'dark' {
    if (typeof window !== 'undefined' && window.matchMedia) {
      return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    return 'light';
  }

  private setupMediaQueryListener(): void {
    if (typeof window !== 'undefined' && window.matchMedia) {
      this.mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
      this.mediaQueryListener = () => {
        const resolved = this.resolveSystemTheme();
        this.systemTheme.set(resolved);
        if (this.themeMode() === ThemeMode.System) {
          this.applyThemeToDom(this.effectiveTheme());
        }
      };

      if (this.mediaQuery.addEventListener) {
        this.mediaQuery.addEventListener('change', this.mediaQueryListener);
      } else if ((this.mediaQuery as any).addListener) {
        (this.mediaQuery as any).addListener(this.mediaQueryListener);
      }
    }
  }

  private applyThemeToDom(theme: 'light' | 'dark'): void {
    if (typeof document !== 'undefined' && document.documentElement) {
      document.documentElement.setAttribute('data-theme', theme);
    }
  }

  private getInitialThemeMode(): ThemeMode {
    const stored = this.safeGetStorage(this.STORAGE_KEY);
    if (stored === ThemeMode.Light || stored === ThemeMode.Dark || stored === ThemeMode.System) {
      return stored as ThemeMode;
    }
    return ThemeMode.Light;
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
