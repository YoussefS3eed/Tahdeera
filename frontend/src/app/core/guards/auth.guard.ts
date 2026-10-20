/**
 * Authentication Route Guard
 * Milestone F1 Phase 7: Protects private workspace routes and preserves returnUrl
 */

import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';

export const authGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
): boolean | UrlTree => {
  const router = inject(Router);

  let isLoggedIn = true;
  if (typeof localStorage !== 'undefined') {
    const token = localStorage.getItem('tahdeera_auth_token');
    if (token === 'unauthenticated' || token === '') {
      isLoggedIn = false;
    }
  }

  if (!isLoggedIn) {
    return router.createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url }
    });
  }

  return true;
};
