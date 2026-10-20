/**
 * Unit Tests for WorkspaceLayoutComponent
 * Milestone F2 / Phase 2 & User Story 2: Workspace Layout Shell Tests
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WorkspaceLayoutComponent } from './workspace-layout.component';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';

describe('WorkspaceLayoutComponent', () => {
  let component: WorkspaceLayoutComponent;
  let fixture: ComponentFixture<WorkspaceLayoutComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceLayoutComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(WorkspaceLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render header, sidebar, and mobile nav', () => {
    const sidebar = fixture.debugElement.query(By.css('app-sidebar'));
    expect(sidebar).toBeTruthy();

    const header = fixture.debugElement.query(By.css('app-header'));
    expect(header).toBeTruthy();

    const mobileNav = fixture.debugElement.query(By.css('app-mobile-nav'));
    expect(mobileNav).toBeTruthy();
  });

  it('should toggle mobile drawer state', () => {
    expect(component.isMobileDrawerOpen).toBeFalse();

    component.toggleMobileDrawer();
    fixture.detectChanges();
    expect(component.isMobileDrawerOpen).toBeTrue();

    const drawer = fixture.debugElement.query(By.css('.mobile-drawer-backdrop'));
    expect(drawer).toBeTruthy();

    component.closeMobileDrawer();
    fixture.detectChanges();
    expect(component.isMobileDrawerOpen).toBeFalse();
  });
});
