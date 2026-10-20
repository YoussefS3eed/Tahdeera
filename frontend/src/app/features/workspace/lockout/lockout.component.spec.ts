import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LockoutComponent } from './lockout.component';
import { provideRouter, Router } from '@angular/router';
import { AuthStore } from '../../../core/stores/auth.store';
import { LocalizationService } from '../../../core/services/localization.service';
import { By } from '@angular/platform-browser';

describe('LockoutComponent', () => {
  let component: LockoutComponent;
  let fixture: ComponentFixture<LockoutComponent>;
  let authStore: AuthStore;
  let router: Router;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [LockoutComponent],
      providers: [
        AuthStore,
        {
          provide: LocalizationService,
          useValue: {
            currentLang: () => 'ar',
            isRtl: () => true
          }
        },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LockoutComponent);
    component = fixture.componentInstance;
    authStore = TestBed.inject(AuthStore);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should render State A (Subscription Expired) with amber theme and reassurance text', () => {
    authStore.workspaces.set([
      {
        id: 'ws_expired',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Expired',
        role: 'Teacher',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 0,
        subscriptionExpiresAt: '2026-10-15T00:00:00Z'
      }
    ]);
    authStore.activeWorkspaceId.set('ws_expired');
    fixture.detectChanges();

    expect(component.lockoutReason()).toBe('SubscriptionExpired');

    const cardAmber = fixture.debugElement.query(By.css('.card-amber'));
    expect(cardAmber).toBeTruthy();

    const textContent = fixture.nativeElement.textContent;
    expect(textContent).toContain('انتهت مدة اشتراك');
    expect(textContent).toContain('محفوظة بالكامل');
    expect(textContent).toContain('248');
    expect(textContent).toContain('12');
  });

  it('should render State B (Admin Suspended) with red theme and administrative text', () => {
    authStore.workspaces.set([
      {
        id: 'ws_suspended',
        name: 'سنتر النور',
        code: 'NOOR',
        status: 'Suspended',
        role: 'Teacher',
        studentCount: 110,
        activeGroupCount: 6,
        subscriptionDaysRemaining: 15,
        subscriptionExpiresAt: '2026-11-05T00:00:00Z'
      }
    ]);
    authStore.activeWorkspaceId.set('ws_suspended');
    fixture.detectChanges();

    expect(component.lockoutReason()).toBe('AdminSuspended');

    const cardRed = fixture.debugElement.query(By.css('.card-red'));
    expect(cardRed).toBeTruthy();

    const textContent = fixture.nativeElement.textContent;
    expect(textContent).toContain('موقوف إدارياً');
  });

  it('should render direct clickable phone links in .ltr-num', () => {
    authStore.workspaces.set([
      {
        id: 'ws_expired',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Expired',
        role: 'Teacher',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 0,
        subscriptionExpiresAt: '2026-10-15T00:00:00Z'
      }
    ]);
    authStore.activeWorkspaceId.set('ws_expired');
    fixture.detectChanges();

    const links = fixture.debugElement.queryAll(By.css('.hotline-link'));
    expect(links.length).toBeGreaterThanOrEqual(2);

    const hrefs = links.map(l => l.nativeElement.getAttribute('href'));
    expect(hrefs).toContain('tel:01272785618');
    expect(hrefs).toContain('tel:01019482687');

    const hasLtrNum = links.every(l => l.nativeElement.classList.contains('ltr-num'));
    expect(hasLtrNum).toBeTrue();
  });

  it('should have operational buttons disabled', () => {
    authStore.workspaces.set([
      {
        id: 'ws_expired',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Expired',
        role: 'Teacher',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 0,
        subscriptionExpiresAt: '2026-10-15T00:00:00Z'
      }
    ]);
    authStore.activeWorkspaceId.set('ws_expired');
    fixture.detectChanges();

    const restrictedButtons = fixture.debugElement.queryAll(By.css('.restricted-btn'));
    expect(restrictedButtons.length).toBeGreaterThan(0);
    restrictedButtons.forEach(btn => {
      expect(btn.nativeElement.disabled).toBeTrue();
    });
  });

  it('should invoke authStore.logout() and navigate to /login when clicking logout button', () => {
    authStore.workspaces.set([
      {
        id: 'ws_expired',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Expired',
        role: 'Teacher',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 0,
        subscriptionExpiresAt: '2026-10-15T00:00:00Z'
      }
    ]);
    authStore.activeWorkspaceId.set('ws_expired');
    spyOn(authStore, 'logout').and.callThrough();
    fixture.detectChanges();

    const logoutBtn = fixture.debugElement.query(By.css('.logout-btn'));
    expect(logoutBtn).toBeTruthy();
    logoutBtn.nativeElement.click();

    expect(authStore.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });
});
