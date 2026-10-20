/**
 * Workspace Lockout Route Guard
 * Milestone F2: Gating guard enforcing lockout routing for Expired/Suspended accounts
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

export const lockoutGuard: CanActivateFn = (
  _route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
): boolean | UrlTree => {
  const router = inject(Router);
  const authStore = inject(AuthStore);

  const isLockoutRoute =
    state.url.includes('/workspace/lockout') || state.url.includes('/locked');

  if (authStore.isLockedOut()) {
    if (!isLockoutRoute) {
      return router.createUrlTree(['/workspace/lockout']);
    }
    return true;
  }

  // Active workspace should not remain on lockout screen
  if (isLockoutRoute) {
    return router.createUrlTree(['/workspace/dashboard']);
  }

  return true;
};
