/**
 * Unit Tests for MobileNavComponent
 * Milestone F2 / Phase 2 & User Story 2: Mobile Bottom Navigation Tests
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MobileNavComponent } from './mobile-nav.component';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';

describe('MobileNavComponent', () => {
  let component: MobileNavComponent;
  let fixture: ComponentFixture<MobileNavComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MobileNavComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MobileNavComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render 4 primary navigation items and 1 more button', () => {
    const bottomItems = fixture.debugElement.queryAll(By.css('.nav-bottom-item'));
    expect(bottomItems.length).toBe(5);
  });

  it('should open slide-over drawer when More button is clicked', () => {
    expect(component.isDrawerOpen).toBeFalse();

    const moreBtn = fixture.debugElement.query(By.css('.more-btn'));
    moreBtn.nativeElement.click();
    fixture.detectChanges();

    expect(component.isDrawerOpen).toBeTrue();
    const drawer = fixture.debugElement.query(By.css('.drawer-container'));
    expect(drawer).toBeTruthy();
  });

  it('should render 10 secondary navigation items inside drawer', () => {
    component.isDrawerOpen = true;
    fixture.detectChanges();

    const drawerItems = fixture.debugElement.queryAll(By.css('.drawer-nav-item'));
    expect(drawerItems.length).toBe(10);
  });

  it('should render drawer support phone wrapped in ltr-num', () => {
    component.isDrawerOpen = true;
    fixture.detectChanges();

    const phoneEl = fixture.debugElement.query(By.css('.drawer-phone'));
    expect(phoneEl).toBeTruthy();
    expect(phoneEl.nativeElement.textContent).toBe('01272785618');
    expect(phoneEl.nativeElement.classList.contains('ltr-num')).toBeTrue();
  });

  it('should close drawer when close button is clicked', () => {
    component.isDrawerOpen = true;
    fixture.detectChanges();

    const closeBtn = fixture.debugElement.query(By.css('.btn-close-drawer'));
    closeBtn.nativeElement.click();
    fixture.detectChanges();

    expect(component.isDrawerOpen).toBeFalse();
  });
});
