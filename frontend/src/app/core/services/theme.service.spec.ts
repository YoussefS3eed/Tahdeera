import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';
import { ThemeMode } from '../enums/domain.enums';

describe('ThemeService', () => {
  let service: ThemeService;
  let mockMatchMediaMatches = false;
  let mediaQueryListeners: Array<() => void> = [];

  beforeEach(() => {
    localStorage.clear();
    mockMatchMediaMatches = false;
    mediaQueryListeners = [];

    // Mock matchMedia
    window.matchMedia = (query: string) => ({
      matches: mockMatchMediaMatches,
      media: query,
      onchange: null,
      addListener: (fn: () => void) => mediaQueryListeners.push(fn),
      removeListener: () => {},
      addEventListener: (_event: string, fn: () => void) => mediaQueryListeners.push(fn),
      removeEventListener: () => {},
      dispatchEvent: () => true
    } as any);

    TestBed.configureTestingModule({
      providers: [ThemeService]
    });
    service = TestBed.inject(ThemeService);
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  it('should initialize with default Light theme when localStorage is empty', () => {
    expect(service.themeMode()).toBe(ThemeMode.Light);
    expect(service.effectiveTheme()).toBe('light');
    expect(service.isDarkMode()).toBeFalse();
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it('should switch to Dark mode, update DOM, and persist to localStorage', () => {
    service.setTheme(ThemeMode.Dark);

    expect(service.themeMode()).toBe(ThemeMode.Dark);
    expect(service.effectiveTheme()).toBe('dark');
    expect(service.isDarkMode()).toBeTrue();
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(localStorage.getItem('tahdeera_theme')).toBe('dark');
  });

  it('should resolve System mode based on prefers-color-scheme', () => {
    mockMatchMediaMatches = true; // OS prefers dark
    service.setTheme(ThemeMode.System);

    expect(service.themeMode()).toBe(ThemeMode.System);
    expect(service.effectiveTheme()).toBe('dark');
    expect(service.isDarkMode()).toBeTrue();
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('should toggle from Light to Dark, and Dark to Light in manual mode', () => {
    service.setTheme(ThemeMode.Light);
    service.toggleTheme();
    expect(service.themeMode()).toBe(ThemeMode.Dark);
    expect(service.effectiveTheme()).toBe('dark');

    service.toggleTheme();
    expect(service.themeMode()).toBe(ThemeMode.Light);
    expect(service.effectiveTheme()).toBe('light');
  });

  it('should flip to the opposite of effective theme when toggling from System mode and exit System mode', () => {
    // Case 1: System mode currently resolving to dark -> flipping must set explicit Light mode
    mockMatchMediaMatches = true; // OS prefers dark
    service.setTheme(ThemeMode.System);
    expect(service.effectiveTheme()).toBe('dark');

    service.toggleTheme();
    expect(service.themeMode()).toBe(ThemeMode.Light);
    expect(service.effectiveTheme()).toBe('light');
    expect(localStorage.getItem('tahdeera_theme')).toBe('light');

    // Case 2: System mode currently resolving to light -> flipping must set explicit Dark mode
    mockMatchMediaMatches = false; // OS prefers light
    service.setTheme(ThemeMode.System);
    expect(service.effectiveTheme()).toBe('light');

    service.toggleTheme();
    expect(service.themeMode()).toBe(ThemeMode.Dark);
    expect(service.effectiveTheme()).toBe('dark');
    expect(localStorage.getItem('tahdeera_theme')).toBe('dark');
  });

  it('should respond to OS matchMedia changes when in System mode', () => {
    service.setTheme(ThemeMode.System);
    expect(service.effectiveTheme()).toBe('light');

    // Simulate OS switching to dark
    mockMatchMediaMatches = true;
    mediaQueryListeners.forEach(listener => listener());

    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });
});
