/**
 * Role Authorization Route Guard
 * Milestone F2 Phase 7: Validates user WorkspaceRole against route requirements
 * Denied access redirects to /workspace/dashboard with one-time router state
 */

import { inject } from '@angular/core';
import {
  CanActivateFn,
  Router,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  UrlTree
} from '@angular/router';
import { AuthStore } from '../stores/auth.store';
import { WorkspaceRole } from '../models/auth.models';

export const roleGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
): boolean | UrlTree => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  if (!authStore.isAuthenticated()) {
    return router.createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url }
    });
  }

  const requiredRoles = route.data?.['requiredRoles'] as WorkspaceRole[] | undefined;

  if (!requiredRoles || requiredRoles.length === 0) {
    return true;
  }

  const currentRole = authStore.currentUserRole();

  // Universal clearance for SuperAdmin or matching workspace role
  if (
    currentRole === ('SuperAdmin' as unknown as WorkspaceRole) ||
    (currentRole && requiredRoles.includes(currentRole))
  ) {
    return true;
  }

  // Deny access and redirect to dashboard with client-side one-time router state
  router.navigate(['/workspace/dashboard'], {
    state: {
      accessDenied: true,
      messageKey: 'DASHBOARD.UNAUTHORIZED_ROLE_ACCESS',
      attemptedUrl: state.url,
      timestamp: Date.now()
    }
  });

  return false;
};
