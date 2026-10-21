import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { superAdminGuard } from './super-admin.guard';
import { AuthStore } from '../stores/auth.store';

describe('superAdminGuard', () => {
  let router: jasmine.SpyObj<Router>;
  let authStore: AuthStore;

  beforeEach(() => {
    router = jasmine.createSpyObj<Router>('Router', ['createUrlTree']);
    TestBed.configureTestingModule({
      providers: [
        AuthStore,
        { provide: Router, useValue: router }
      ]
    });
    authStore = TestBed.inject(AuthStore);
  });

  it('should redirect unauthenticated users to /login with returnUrl', () => {
    authStore.user.set(null);
    authStore.accessToken.set(null);

    const mockRoute = {} as ActivatedRouteSnapshot;
    const mockState = { url: '/admin/overview' } as RouterStateSnapshot;
    const mockUrlTree = {} as UrlTree;
    router.createUrlTree.and.returnValue(mockUrlTree);

    const result = TestBed.runInInjectionContext(() => superAdminGuard(mockRoute, mockState));

    expect(result).toBe(mockUrlTree);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/admin/overview' }
    });
  });

  it('should redirect authenticated non-admin users to /forbidden', () => {
    authStore.user.set({
      id: 'usr_teacher',
      fullName: 'أ. محمد',
      email: 'teacher@tahdeera.app',
      phone: '01012345670',
      initials: 'م.ع',
      isPlatformAdmin: false
    });
    authStore.accessToken.set('valid_teacher_token');

    const mockRoute = {} as ActivatedRouteSnapshot;
    const mockState = { url: '/admin/accounts' } as RouterStateSnapshot;
    const mockUrlTree = {} as UrlTree;
    router.createUrlTree.and.returnValue(mockUrlTree);

    const result = TestBed.runInInjectionContext(() => superAdminGuard(mockRoute, mockState));

    expect(result).toBe(mockUrlTree);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/forbidden']);
  });

  it('should allow access to platform admin users', () => {
    authStore.user.set({
      id: 'usr_admin',
      fullName: 'مدير المنصة العام',
      email: 'admin@tahdeera.app',
      phone: '01272785618',
      initials: 'م.م',
      isPlatformAdmin: true
    });
    authStore.accessToken.set('valid_admin_token');

    const mockRoute = {} as ActivatedRouteSnapshot;
    const mockState = { url: '/admin/overview' } as RouterStateSnapshot;

    const result = TestBed.runInInjectionContext(() => superAdminGuard(mockRoute, mockState));

    expect(result).toBe(true);
  });
});
