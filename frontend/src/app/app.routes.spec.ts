import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideLocationMocks } from '@angular/common/testing';
import { Location } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { routes } from './app.routes';
import { AuthStore } from './core/stores/auth.store';

describe('App Routes Integration', () => {
  let router: Router;
  let location: Location;
  let authStore: AuthStore;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      providers: [
        AuthStore,
        provideRouter(routes),
        provideLocationMocks(),
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    location = TestBed.inject(Location);
    authStore = TestBed.inject(AuthStore);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should navigate to root / successfully', async () => {
    await router.navigateByUrl('/');
    expect(['', '/']).toContain(location.path());
  });

  it('should navigate to /login successfully', async () => {
    await router.navigateByUrl('/login');
    expect(location.path()).toBe('/login');
  });

  it('should redirect unauthenticated access from /workspace/dashboard to /login with returnUrl', async () => {
    authStore.accessToken.set(null);
    authStore.workspaces.set([]);

    await router.navigateByUrl('/workspace/dashboard');
    expect(location.path()).toContain('/login?returnUrl=');
  });

  it('should redirect locked account from /workspace/dashboard to /workspace/lockout', async () => {
    authStore.accessToken.set('valid_token');
    authStore.user.set({
      id: 'usr_01',
      fullName: 'أ. محمد',
      email: 'teacher@tahdeera.app',
      phone: '01012345670',
      initials: 'م.ع'
    });
    authStore.workspaces.set([
      {
        id: 'ws_locked',
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
    authStore.activeWorkspaceId.set('ws_locked');

    await router.navigateByUrl('/workspace/dashboard');
    expect(location.path()).toBe('/workspace/lockout');
  });

  it('should resolve /workspace/dashboard when authenticated and workspace is active', async () => {
    authStore.accessToken.set('valid_token');
    authStore.user.set({
      id: 'usr_01',
      fullName: 'أ. محمد',
      email: 'teacher@tahdeera.app',
      phone: '01012345670',
      initials: 'م.ع'
    });
    authStore.workspaces.set([
      {
        id: 'ws_active',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Active',
        role: 'Teacher',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 45,
        subscriptionExpiresAt: '2026-12-05T00:00:00Z'
      }
    ]);
    authStore.activeWorkspaceId.set('ws_active');

    await router.navigateByUrl('/workspace/dashboard');
    expect(location.path()).toBe('/workspace/dashboard');
  });

  it('should redirect /locked to /workspace/lockout when authenticated', async () => {
    authStore.accessToken.set('valid_token');
    authStore.user.set({
      id: 'usr_01',
      fullName: 'أ. محمد',
      email: 'teacher@tahdeera.app',
      phone: '01012345670',
      initials: 'م.ع'
    });
    authStore.workspaces.set([
      {
        id: 'ws_locked',
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
    authStore.activeWorkspaceId.set('ws_locked');

    await router.navigateByUrl('/locked');
    expect(location.path()).toBe('/workspace/lockout');
  });

  it('should redirect unknown wildcard routes to root /', async () => {
    await router.navigateByUrl('/unknown-route-12345');
    expect(['', '/']).toContain(location.path());
  });
});
