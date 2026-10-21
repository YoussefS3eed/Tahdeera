import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { AdminImpersonationStore, ADMIN_ROOT_SESSION_KEY } from './admin-impersonation.store';
import { AuthStore } from './auth.store';
import { AdminStore } from './admin.store';
import { MOCK_ADMIN_WORKSPACES } from '../mocks/admin-mock-data';

describe('AdminImpersonationStore', () => {
  let impersonationStore: AdminImpersonationStore;
  let authStore: AuthStore;
  let adminStore: AdminStore;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        AdminImpersonationStore,
        AuthStore,
        AdminStore,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: router }
      ]
    });

    impersonationStore = TestBed.inject(AdminImpersonationStore);
    authStore = TestBed.inject(AuthStore);
    adminStore = TestBed.inject(AdminStore);
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('should start impersonation, backup root session, and route to /workspace/dashboard', () => {
    authStore.user.set({
      id: 'usr_root',
      fullName: 'Super Admin',
      email: 'admin@tahdeera.app',
      phone: '01272785618',
      initials: 'SA',
      isPlatformAdmin: true
    });
    authStore.accessToken.set('root_token_123');

    const target = MOCK_ADMIN_WORKSPACES[0];
    impersonationStore.startImpersonation(target);

    expect(impersonationStore.isImpersonating()).toBeTrue();
    expect(impersonationStore.targetWorkspace()).toEqual(target);
    expect(sessionStorage.getItem(ADMIN_ROOT_SESSION_KEY)).toBeTruthy();
    expect(authStore.activeWorkspace()?.id).toBe(target.id);
    expect(router.navigate).toHaveBeenCalledWith(['/workspace/dashboard']);
  });

  it('should exit impersonation, restore root session, and route back to /admin/accounts', () => {
    const target = MOCK_ADMIN_WORKSPACES[0];
    authStore.user.set({
      id: 'usr_root',
      fullName: 'Super Admin',
      email: 'admin@tahdeera.app',
      phone: '01272785618',
      initials: 'SA',
      isPlatformAdmin: true
    });
    authStore.accessToken.set('root_token_123');

    impersonationStore.startImpersonation(target);
    expect(impersonationStore.isImpersonating()).toBeTrue();

    impersonationStore.exitImpersonation();
    expect(impersonationStore.isImpersonating()).toBeFalse();
    expect(sessionStorage.getItem(ADMIN_ROOT_SESSION_KEY)).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/admin/accounts']);
  });
});
