/**
 * Unit Tests for LandingComponent (SCR-01)
 * Milestone F2 / Phase 3: Public Landing Portal Tests
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LandingComponent } from './landing.component';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { LocalizationService } from '../../../core/services/localization.service';
import { AuthStore } from '../../../core/stores/auth.store';
import { ThemeService } from '../../../core/services/theme.service';
import { SupportedLanguage } from '../../../core/enums/domain.enums';

describe('LandingComponent', () => {
  let component: LandingComponent;
  let fixture: ComponentFixture<LandingComponent>;
  let localizationService: LocalizationService;
  let themeService: ThemeService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LandingComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        AuthStore
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LandingComponent);
    component = fixture.componentInstance;
    localizationService = TestBed.inject(LocalizationService);
    themeService = TestBed.inject(ThemeService);
    fixture.detectChanges();
  });

  it('should render the hero section with simulated NFC tap preview', () => {
    const heroTitle = fixture.debugElement.query(By.css('.hero-headline'));
    expect(heroTitle).toBeTruthy();

    const nfcTitle = fixture.debugElement.query(By.css('.nfc-tap-title'));
    expect(nfcTitle).toBeTruthy();
    expect(nfcTitle.nativeElement.textContent).toContain('04:A2:7C:19');

    const ltrCard = fixture.debugElement.query(By.css('.nfc-tap-title .ltr-num'));
    expect(ltrCard).toBeTruthy();
  });

  it('should render 4 capability pillars in the features grid', () => {
    const capabilityCards = fixture.debugElement.queryAll(By.css('.capability-card'));
    expect(capabilityCards.length).toBe(4);
  });

  it('should render 3 package comparison cards with ltr-num pricing', () => {
    const packageCards = fixture.debugElement.queryAll(By.css('.package-card'));
    expect(packageCards.length).toBe(3);

    const prices = fixture.debugElement.queryAll(By.css('.pkg-amount.ltr-num'));
    expect(prices.length).toBe(3);
    const priceTexts = prices.map(p => p.nativeElement.textContent.trim());
    expect(priceTexts).toContain('199');
    expect(priceTexts).toContain('349');
    expect(priceTexts).toContain('599');
  });

  it('should open subscription modal when CTA is clicked', () => {
    expect(component.isModalOpen).toBeFalse();

    const ctaButton = fixture.debugElement.query(By.css('.btn-hero-primary'));
    ctaButton.nativeElement.click();
    fixture.detectChanges();

    expect(component.isModalOpen).toBeTrue();
  });

  it('should render contact footer with clickable hotlines wrapped in ltr-num', () => {
    const hotlineLinks = fixture.debugElement.queryAll(By.css('.hotline-link'));
    expect(hotlineLinks.length).toBe(2);

    expect(hotlineLinks[0].nativeElement.getAttribute('href')).toBe('tel:01272785618');
    expect(hotlineLinks[0].nativeElement.classList.contains('ltr-num')).toBeTrue();

    expect(hotlineLinks[1].nativeElement.getAttribute('href')).toBe('tel:01019482687');
    expect(hotlineLinks[1].nativeElement.classList.contains('ltr-num')).toBeTrue();
  });

  it('should toggle language when language switcher is clicked', () => {
    spyOn(localizationService, 'toggleLanguage');

    const langBtn = fixture.debugElement.query(By.css('.icon-action-btn'));
    langBtn.nativeElement.click();

    expect(localizationService.toggleLanguage).toHaveBeenCalled();
  });

  it('should toggle theme when theme switcher is clicked', () => {
    spyOn(themeService, 'toggleTheme');

    const themeBtns = fixture.debugElement.queryAll(By.css('.icon-action-btn'));
    // Second icon button is the theme button
    themeBtns[1].nativeElement.click();

    expect(themeService.toggleTheme).toHaveBeenCalled();
  });

  it('should toggle FAQ accordion items', () => {

  });

  it('should render login CTA when unauthenticated', () => {
    const authStore = TestBed.inject(AuthStore);
    authStore.logout();
    fixture.detectChanges();

    const cta = fixture.debugElement.query(By.css('.btn-cta-secondary'));
    expect(cta).toBeTruthy();
    expect(cta.nativeElement.textContent).toContain('تسجيل الدخول');
  });

  it('should render Go to Dashboard CTA when authenticated', () => {
    const authStore = TestBed.inject(AuthStore);
    authStore.login({
      user: { id: 'usr_01', fullName: 'أ. محمد', email: 't@t.app', phone: '01012345670', initials: 'م.ع' },
      tokens: { accessToken: 'tk', refreshToken: 'rt', tokenType: 'Bearer', expiresIn: 900 },
      workspaces: [{ id: 'ws_1', name: 'سنتر الأوائل', code: 'AW', status: 'Active', role: 'Teacher', studentCount: 248, activeGroupCount: 12, subscriptionDaysRemaining: 45, subscriptionExpiresAt: '2026-12-05' }],
      activeWorkspaceId: 'ws_1'
    });
    fixture.detectChanges();

    const dashboardCta = fixture.debugElement.query(By.css('.btn-cta-dashboard'));
    expect(dashboardCta).toBeTruthy();
    expect(dashboardCta.nativeElement.textContent).toContain('الانتقال إلى لوحة التحكم');

  });
});
