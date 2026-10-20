/**
 * Workspace Lockout Route Guard
 * Milestone F1 Phase 7: Protects against Expired or Suspended workspace access
 */

import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';

export const lockoutGuard: CanActivateFn = (
  _route: ActivatedRouteSnapshot,
  _state: RouterStateSnapshot
): boolean | UrlTree => {
  const router = inject(Router);

  let status = 'Active';
  if (typeof localStorage !== 'undefined') {
    status = localStorage.getItem('tahdeera_workspace_status') || 'Active';
  }

  if (status === 'Expired' || status === 'Suspended') {
    return router.createUrlTree(['/workspace/lockout']);
  }

  return true;
};
