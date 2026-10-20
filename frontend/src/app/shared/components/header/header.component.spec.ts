/**
 * Unit Tests for HeaderComponent
 * Milestone F2 / Phase 2 & User Story 2: Universal Header Component Tests
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HeaderComponent } from './header.component';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { LocalizationService } from '../../../core/services/localization.service';
import { ThemeService } from '../../../core/services/theme.service';

describe('HeaderComponent', () => {
  let component: HeaderComponent;
  let fixture: ComponentFixture<HeaderComponent>;
  let localizationService: LocalizationService;
  let themeService: ThemeService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HeaderComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HeaderComponent);
    component = fixture.componentInstance;
    localizationService = TestBed.inject(LocalizationService);
    themeService = TestBed.inject(ThemeService);
    fixture.detectChanges();
  });

  it('should render workspace badge', () => {
    component.workspaceName = 'سنتر الأوائل';
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.workspace-name'));
    expect(badge).toBeTruthy();
    expect(badge.nativeElement.textContent).toContain('سنتر الأوائل');
  });

  it('should open global search modal when search button is clicked', () => {
    expect(component.isSearchOpen).toBeFalse();

    component.openSearchModal();
    fixture.detectChanges();

    expect(component.isSearchOpen).toBeTrue();
  });

  it('should toggle language when language button is clicked', () => {
    spyOn(localizationService, 'toggleLanguage');

    const langBtn = fixture.debugElement.query(By.css('button[aria-label="تغيير اللغة"]'));
    langBtn.nativeElement.click();

    expect(localizationService.toggleLanguage).toHaveBeenCalled();
  });

  it('should toggle theme when theme button is clicked', () => {
    spyOn(themeService, 'toggleTheme');

    const themeBtn = fixture.debugElement.query(By.css('button[aria-label="تبديل المظهر"]'));
    themeBtn.nativeElement.click();

    expect(themeService.toggleTheme).toHaveBeenCalled();
  });

  it('should emit toggleSidebar when mobile menu button is clicked', () => {
    spyOn(component.toggleSidebar, 'emit');

    const menuBtn = fixture.debugElement.query(By.css('.mobile-menu-btn'));
    menuBtn.nativeElement.click();

    expect(component.toggleSidebar.emit).toHaveBeenCalled();
  });
});
