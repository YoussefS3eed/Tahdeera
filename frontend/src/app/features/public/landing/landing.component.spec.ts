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
import { ThemeService } from '../../../core/services/theme.service';

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
        provideRouter([])
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
    expect(component.faqItems[0].isOpen).toBeFalse();

    component.toggleFaq(0);
    expect(component.faqItems[0].isOpen).toBeTrue();

    component.toggleFaq(0);
    expect(component.faqItems[0].isOpen).toBeFalse();
  });
});
