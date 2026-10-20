/**
 * Unit Tests for GlobalSearchModalComponent
 * Milestone F2 / SCR-01 & User Story 2: Global Search Modal Stub Tests
 */

import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { GlobalSearchModalComponent } from './global-search-modal.component';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { By } from '@angular/platform-browser';
import { LocalizationService } from '../../../core/services/localization.service';
import { signal } from '@angular/core';

describe('GlobalSearchModalComponent', () => {
  let component: GlobalSearchModalComponent;
  let fixture: ComponentFixture<GlobalSearchModalComponent>;
  let httpTesting: HttpTestingController;

  const mockLocalizationService = {
    isRtl: signal(true),
    direction: signal<'rtl' | 'ltr'>('rtl'),
    translate: (key: string) => {
      const dict: Record<string, string> = {
        'SHELL.SEARCH_PLACEHOLDER': 'بحث سريع (Ctrl+K)...',
        'SHELL.SEARCH_MODAL_TITLE': 'البحث الشامل',
        'SHELL.SEARCH_MODAL_CLOSE': 'إغلاق'
      };
      return dict[key] || key;
    }
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GlobalSearchModalComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: LocalizationService, useValue: mockLocalizationService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GlobalSearchModalComponent);
    component = fixture.componentInstance;
    httpTesting = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpTesting.verify(); // Strictly verifies ZERO network calls were initiated
  });

  it('should not display dialog when isOpen is false', () => {
    component.isOpen = false;
    fixture.detectChanges();

    const dialog = fixture.debugElement.query(By.css('.search-dialog'));
    expect(dialog).toBeNull();
  });

  it('should open dialog and focus input when openModal is called', fakeAsync(() => {
    component.openModal();
    fixture.detectChanges();

    const dialog = fixture.debugElement.query(By.css('.search-dialog'));
    expect(dialog).toBeTruthy();
    expect(dialog.attributes['role']).toBe('dialog');
    expect(dialog.attributes['aria-modal']).toBe('true');

    tick(60);
    const input = fixture.debugElement.query(By.css('#global-search-input'));
    expect(input).toBeTruthy();
    expect(document.activeElement).toBe(input.nativeElement);
  }));

  it('should display the F3 informational notice message', () => {
    component.isOpen = true;
    fixture.detectChanges();

    const noticeBanner = fixture.debugElement.query(By.css('.info-notice-banner'));
    expect(noticeBanner).toBeTruthy();
    expect(noticeBanner.nativeElement.textContent).toContain('F3');
  });

  it('should open on Ctrl+K global shortcut', fakeAsync(() => {
    expect(component.isOpen).toBeFalse();

    const event = new KeyboardEvent('keydown', {
      key: 'k',
      ctrlKey: true,
      bubbles: true,
      cancelable: true
    });
    window.dispatchEvent(event);
    fixture.detectChanges();

    expect(component.isOpen).toBeTrue();
  }));

  it('should close on Escape keydown', fakeAsync(() => {
    component.openModal();
    fixture.detectChanges();
    expect(component.isOpen).toBeTrue();

    const event = new KeyboardEvent('keydown', {
      key: 'Escape',
      bubbles: true,
      cancelable: true
    });
    window.dispatchEvent(event);
    fixture.detectChanges();

    expect(component.isOpen).toBeFalse();
  }));

  it('should suppress Ctrl+K shortcut when user is typing in an external input', () => {
    expect(component.isOpen).toBeFalse();

    const externalInput = document.createElement('input');
    document.body.appendChild(externalInput);
    externalInput.focus();

    const event = new KeyboardEvent('keydown', {
      key: 'k',
      ctrlKey: true,
      bubbles: true,
      cancelable: true
    });
    externalInput.dispatchEvent(event);
    fixture.detectChanges();

    expect(component.isOpen).toBeFalse();
    document.body.removeChild(externalInput);
  });
});
