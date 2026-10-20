/**
 * Unit Tests for Route Guards
 * Milestone F1 Phase 7: authGuard, roleGuard, lockoutGuard Tests
 */

import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { authGuard } from './auth.guard';
import { roleGuard } from './role.guard';
import { lockoutGuard } from './lockout.guard';
import { WorkspaceRole } from '../enums/domain.enums';

describe('Route Guards', () => {
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    router = jasmine.createSpyObj('Router', ['createUrlTree']);
    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: router }
      ]
    });
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('authGuard', () => {
    it('should allow access when user is authenticated', () => {
      localStorage.setItem('tahdeera_auth_token', 'valid-token');
      const route = {} as ActivatedRouteSnapshot;
      const state = { url: '/workspace/dashboard' } as RouterStateSnapshot;

      const result = TestBed.runInInjectionContext(() => authGuard(route, state));
      expect(result).toBeTrue();
    });

    it('should redirect to /login with returnUrl when unauthenticated', () => {
      localStorage.setItem('tahdeera_auth_token', 'unauthenticated');
      const route = {} as ActivatedRouteSnapshot;
      const state = { url: '/workspace/dashboard' } as RouterStateSnapshot;

      TestBed.runInInjectionContext(() => authGuard(route, state));
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], {
        queryParams: { returnUrl: '/workspace/dashboard' }
      });
    });
  });

  describe('roleGuard', () => {
    it('should allow user with required role', () => {
      localStorage.setItem('tahdeera_user_role', WorkspaceRole.Teacher);
      const route = {
        data: { requiredRoles: [WorkspaceRole.Teacher, WorkspaceRole.Owner] }
      } as unknown as ActivatedRouteSnapshot;
      const state = {} as RouterStateSnapshot;

      const result = TestBed.runInInjectionContext(() => roleGuard(route, state));
      expect(result).toBeTrue();
    });

    it('should redirect to /workspace/forbidden when role is insufficient', () => {
      localStorage.setItem('tahdeera_user_role', WorkspaceRole.Assistant);
      const route = {
        data: { requiredRoles: [WorkspaceRole.Owner] }
      } as unknown as ActivatedRouteSnapshot;
      const state = {} as RouterStateSnapshot;

      TestBed.runInInjectionContext(() => roleGuard(route, state));
      expect(router.createUrlTree).toHaveBeenCalledWith(['/workspace/forbidden']);
    });

    it('should allow SuperAdmin regardless of route role requirements', () => {
      localStorage.setItem('tahdeera_user_role', 'SuperAdmin');
      const route = {
        data: { requiredRoles: [WorkspaceRole.Admin] }
      } as unknown as ActivatedRouteSnapshot;
      const state = {} as RouterStateSnapshot;

      const result = TestBed.runInInjectionContext(() => roleGuard(route, state));
      expect(result).toBeTrue();
    });
  });

  describe('lockoutGuard', () => {
    it('should allow access when workspace is Active', () => {
      localStorage.setItem('tahdeera_workspace_status', 'Active');
      const route = {} as ActivatedRouteSnapshot;
      const state = {} as RouterStateSnapshot;

      const result = TestBed.runInInjectionContext(() => lockoutGuard(route, state));
      expect(result).toBeTrue();
    });

    it('should redirect to /workspace/lockout when workspace is Expired', () => {
      localStorage.setItem('tahdeera_workspace_status', 'Expired');
      const route = {} as ActivatedRouteSnapshot;
      const state = {} as RouterStateSnapshot;

      TestBed.runInInjectionContext(() => lockoutGuard(route, state));
      expect(router.createUrlTree).toHaveBeenCalledWith(['/workspace/lockout']);
    });
  });
});
