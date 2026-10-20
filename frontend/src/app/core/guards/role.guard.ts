/**
 * Role Authorization Route Guard
 * Milestone F1 Phase 7: Validates user WorkspaceRole against route requirements
 */

import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { WorkspaceRole } from '../enums/domain.enums';

export const roleGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  _state: RouterStateSnapshot
): boolean | UrlTree => {
  const router = inject(Router);
  const requiredRoles = route.data?.['requiredRoles'] as WorkspaceRole[] | undefined;

  if (!requiredRoles || requiredRoles.length === 0) {
    return true;
  }

  let currentRole: WorkspaceRole = WorkspaceRole.Teacher;
  if (typeof localStorage !== 'undefined') {
    const savedRole = localStorage.getItem('tahdeera_user_role') as WorkspaceRole;
    if (savedRole && Object.values(WorkspaceRole).includes(savedRole)) {
      currentRole = savedRole;
    }
  }

  // SuperAdmin has universal clearance
  if ((currentRole as string) === 'SuperAdmin' || requiredRoles.includes(currentRole)) {
    return true;
  }

  return router.createUrlTree(['/workspace/forbidden']);
};
