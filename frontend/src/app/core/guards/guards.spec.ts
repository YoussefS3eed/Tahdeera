/**
 * Unit Tests for Route Guards
 * Milestone F2: authGuard, roleGuard, lockoutGuard Tests with AuthStore
 */

import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { authGuard } from './auth.guard';
import { roleGuard } from './role.guard';
import { lockoutGuard } from './lockout.guard';
import { AuthStore } from '../stores/auth.store';
import { WorkspaceRole } from '../models/auth.models';

describe('Route Guards', () => {
  let router: jasmine.SpyObj<Router>;
  let authStore: AuthStore;

  const mockActiveLogin = {
    user: {
      id: 'usr_01',
      fullName: 'أ. محمد',
      email: 'teacher@tahdeera.app',
      phone: '01012345670',
      initials: 'م.ع'
    },
    tokens: {
      accessToken: 'valid_token_123',
      refreshToken: 'refresh_token_123',
      tokenType: 'Bearer' as const,
      expiresIn: 900
    },
    workspaces: [
      {
        id: 'ws_01',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Active' as const,
        role: 'Teacher' as WorkspaceRole,
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 45,
        subscriptionExpiresAt: '2026-12-05T00:00:00Z'
      }
    ],
    activeWorkspaceId: 'ws_01'
  };

  const mockExpiredLogin = {
    user: {
      id: 'usr_02',
      fullName: 'أ. سارة',
      email: 'expired@tahdeera.app',
      phone: '01112345670',
      initials: 'س.أ'
    },
    tokens: {
      accessToken: 'valid_token_expired_ws',
      refreshToken: 'refresh_token_expired_ws',
      tokenType: 'Bearer' as const,
      expiresIn: 900
    },
    workspaces: [
      {
        id: 'ws_exp',
        name: 'سنتر المنتهي',
        code: 'EXP',
        status: 'Expired' as const,
        role: 'Owner' as WorkspaceRole,
        studentCount: 50,
        activeGroupCount: 2,
        subscriptionDaysRemaining: 0,
        subscriptionExpiresAt: '2026-10-01T00:00:00Z'
      }
    ],
    activeWorkspaceId: 'ws_exp'
  };

  beforeEach(() => {
    localStorage.clear();
    router = jasmine.createSpyObj('Router', ['createUrlTree']);
    router.createUrlTree.and.callFake((commands: any[], navExtras?: any) => {
      return { toString: () => commands.join('/') } as unknown as UrlTree;
    });

    TestBed.configureTestingModule({
      providers: [
        AuthStore,
        { provide: Router, useValue: router }
      ]
    });
    authStore = TestBed.inject(AuthStore);
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('authGuard', () => {
    it('should allow access when user is authenticated in AuthStore', () => {
      authStore.login(mockActiveLogin);
      const route = {} as ActivatedRouteSnapshot;
      const state = { url: '/workspace/dashboard' } as RouterStateSnapshot;

      const result = TestBed.runInInjectionContext(() => authGuard(route, state));
      expect(result).toBeTrue();
    });

    it('should redirect to /login with returnUrl when unauthenticated', () => {
      authStore.logout();
      const route = {} as ActivatedRouteSnapshot;
      const state = { url: '/workspace/dashboard' } as RouterStateSnapshot;

      TestBed.runInInjectionContext(() => authGuard(route, state));
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], {
        queryParams: { returnUrl: '/workspace/dashboard' }
      });
    });
  });

  describe('lockoutGuard', () => {
    it('should allow operational access when workspace is Active', () => {
      authStore.login(mockActiveLogin);
      const route = {} as ActivatedRouteSnapshot;
      const state = { url: '/workspace/dashboard' } as RouterStateSnapshot;

      const result = TestBed.runInInjectionContext(() => lockoutGuard(route, state));
      expect(result).toBeTrue();
    });

    it('should redirect to /workspace/lockout when workspace is Expired', () => {
      authStore.login(mockExpiredLogin);
      const route = {} as ActivatedRouteSnapshot;
      const state = { url: '/workspace/dashboard' } as RouterStateSnapshot;

      TestBed.runInInjectionContext(() => lockoutGuard(route, state));
      expect(router.createUrlTree).toHaveBeenCalledWith(['/workspace/lockout']);
    });

    it('should allow staying on /workspace/lockout when workspace is Expired', () => {
      authStore.login(mockExpiredLogin);
      const route = {} as ActivatedRouteSnapshot;
      const state = { url: '/workspace/lockout' } as RouterStateSnapshot;

      const result = TestBed.runInInjectionContext(() => lockoutGuard(route, state));
      expect(result).toBeTrue();
    });

    it('should redirect active accounts away from /workspace/lockout to /workspace/dashboard', () => {
      authStore.login(mockActiveLogin);
      const route = {} as ActivatedRouteSnapshot;
      const state = { url: '/workspace/lockout' } as RouterStateSnapshot;

      TestBed.runInInjectionContext(() => lockoutGuard(route, state));
      expect(router.createUrlTree).toHaveBeenCalledWith(['/workspace/dashboard']);
    });
  });
});
