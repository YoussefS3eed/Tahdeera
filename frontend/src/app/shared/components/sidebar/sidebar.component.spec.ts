/**
 * Unit Tests for SidebarComponent
 * Milestone F2 / Phase 2 & User Story 2: Desktop Sidebar Component Tests
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SidebarComponent } from './sidebar.component';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';

describe('SidebarComponent', () => {
  let component: SidebarComponent;
  let fixture: ComponentFixture<SidebarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SidebarComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SidebarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render exactly 14 navigation tabs', () => {
    const tabs = fixture.debugElement.queryAll(By.css('.nav-tab-item'));
    expect(tabs.length).toBe(14);
  });

  it('should render support card with phone number wrapped in ltr-num', () => {
    const phoneEl = fixture.debugElement.query(By.css('.support-card-phone'));
    expect(phoneEl).toBeTruthy();
    expect(phoneEl.nativeElement.textContent).toBe('01272785618');
    expect(phoneEl.nativeElement.classList.contains('ltr-num')).toBeTrue();
  });

  it('should emit tabSelected when a tab is clicked', () => {
    spyOn(component.tabSelected, 'emit');

    const firstTab = fixture.debugElement.query(By.css('.nav-tab-item'));
    firstTab.nativeElement.click();

    expect(component.tabSelected.emit).toHaveBeenCalledWith('dashboard');
  });
});
