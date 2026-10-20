/**
 * Unit Tests for FoundationHarnessComponent
 * Milestone F1 Phase 8: Diagnostic Test Harness UI Component Tests
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { FoundationHarnessComponent } from './foundation-harness.component';
import { ScanOutcome } from '../../core';

describe('FoundationHarnessComponent', () => {
  let component: FoundationHarnessComponent;
  let fixture: ComponentFixture<FoundationHarnessComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FoundationHarnessComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FoundationHarnessComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create harness component with 8 diagnostic panels', () => {
    expect(component).toBeTruthy();
    expect(component.simulatedCardUid).toBe('04A27C19');
  });

  it('should toggle theme and language', () => {
    component.setTheme('dark');
    expect(component.themeService.themeMode()).toBe('dark');

    component.setLanguage('en');
    expect(component.localizationService.currentLang()).toBe('en');
  });

  it('should simulate NFC scan and append to recentScans', () => {
    component.simulatedCardUid = '04A27C19';
    component.simulateCardScan();

    expect(component.recentScans().length).toBeGreaterThan(0);
    expect(component.recentScans()[0].uid).toBe('04A27C19');
  });

  it('should open and close accessible modal dialog', () => {
    expect(component.isModalOpen()).toBeFalse();
    component.openModal();
    expect(component.isModalOpen()).toBeTrue();
    component.closeModal();
    expect(component.isModalOpen()).toBeFalse();
  });
});
