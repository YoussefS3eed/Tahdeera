import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, UrlSegment } from '@angular/router';
import { By } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { WorkspacePlaceholderComponent } from './workspace-placeholder.component';
import { LocalizationService } from '../../../core/services/localization.service';
import { DEFERRED_WORKSPACE_TABS } from './placeholder.models';

describe('WorkspacePlaceholderComponent', () => {
  let routerSpy: jasmine.SpyObj<Router>;
  let localizationService: LocalizationService;

  function createComponentWithSlug(slug: string): {
    fixture: ComponentFixture<WorkspacePlaceholderComponent>;
    component: WorkspacePlaceholderComponent;
  } {
    const mockActivatedRoute = {
      snapshot: {
        url: [new UrlSegment(slug, {})],
        routeConfig: { path: slug }
      }
    };

    TestBed.resetTestingModule();
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [WorkspacePlaceholderComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        LocalizationService,
        { provide: ActivatedRoute, useValue: mockActivatedRoute },
        { provide: Router, useValue: routerSpy }
      ]
    });

    localizationService = TestBed.inject(LocalizationService);
    // ensure Arabic default
    if (localizationService.currentLang() !== 'ar') {
      localizationService.toggleLanguage();
    }

    const fixture = TestBed.createComponent(WorkspacePlaceholderComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    return { fixture, component };
  }

  it('should resolve Milestone F3 badge and Arabic title for nfc-station', () => {
    const { fixture, component } = createComponentWithSlug('nfc-station');
    expect(component.targetMilestone()).toBe('Milestone F3');
    expect(component.badgeColorClass()).toBe('badge-f3');
    expect(component.title()).toBe(DEFERRED_WORKSPACE_TABS['nfc-station'].titleAr);

    const badgeEl = fixture.debugElement.query(By.css('.badge-notice'));
    expect(badgeEl.nativeElement.textContent).toContain('Milestone F3');
    expect(badgeEl.nativeElement.classList).toContain('badge-f3');

    const titleEl = fixture.debugElement.query(By.css('.placeholder-title'));
    expect(titleEl.nativeElement.textContent).toContain('محطة الحضور المباشر NFC');
  });

  it('should resolve Milestone F3 for all F3 deferred routes', () => {
    const f3Routes = ['nfc-station', 'attendance', 'students', 'cards', 'groups', 'schedule'];
    for (const route of f3Routes) {
      const { component } = createComponentWithSlug(route);
      expect(component.targetMilestone()).toBe('Milestone F3');
      expect(component.badgeColorClass()).toBe('badge-f3');
      expect(component.title()).toBe(DEFERRED_WORKSPACE_TABS[route].titleAr);
    }
  });

  it('should resolve Milestone F4 badge and Arabic title for payments', () => {
    const { fixture, component } = createComponentWithSlug('payments');
    expect(component.targetMilestone()).toBe('Milestone F4');
    expect(component.badgeColorClass()).toBe('badge-f4');
    expect(component.title()).toBe(DEFERRED_WORKSPACE_TABS['payments'].titleAr);

    const badgeEl = fixture.debugElement.query(By.css('.badge-notice'));
    expect(badgeEl.nativeElement.textContent).toContain('Milestone F4');
    expect(badgeEl.nativeElement.classList).toContain('badge-f4');

    const titleEl = fixture.debugElement.query(By.css('.placeholder-title'));
    expect(titleEl.nativeElement.textContent).toContain('سجل المصروفات والمدفوعات');
  });

  it('should resolve Milestone F4 for all F4 deferred routes', () => {
    const f4Routes = ['payments', 'whatsapp', 'lms', 'reports', 'team', 'settings', 'ai-assistant'];
    for (const route of f4Routes) {
      const { component } = createComponentWithSlug(route);
      expect(component.targetMilestone()).toBe('Milestone F4');
      expect(component.badgeColorClass()).toBe('badge-f4');
      expect(component.title()).toBe(DEFERRED_WORKSPACE_TABS[route].titleAr);
    }
  });

  it('should toggle English title and description when language changes', () => {
    const { fixture, component } = createComponentWithSlug('students');
    expect(component.title()).toBe('سجل وملفات الطلاب');

    localizationService.toggleLanguage();
    fixture.detectChanges();

    expect(component.title()).toBe('Student Directory & Profiles');
    expect(component.description()).toBe(DEFERRED_WORKSPACE_TABS['students'].descriptionEn);
  });

  it('should navigate to /workspace/dashboard when return button is clicked', () => {
    const { fixture } = createComponentWithSlug('attendance');
    const returnBtn = fixture.debugElement.query(By.css('.btn-return'));
    expect(returnBtn).toBeTruthy();

    returnBtn.nativeElement.click();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/workspace/dashboard']);
  });

  it('should render fallback content for unknown route slug', () => {
    const { fixture, component } = createComponentWithSlug('unknown-route-slug');
    expect(component.title()).toBe('قسم قيد التطوير');
    expect(component.targetMilestone()).toBe('Milestone F3');

    const titleEl = fixture.debugElement.query(By.css('.placeholder-title'));
    expect(titleEl.nativeElement.textContent).toContain('قسم قيد التطوير');
  });
});
