import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthStore } from './auth.store';
import { AdminStore } from './admin.store';
import { AdminWorkspaceSummary } from '../models/admin.models';

export const ADMIN_ROOT_SESSION_KEY = 'tahdeera_admin_root_session';

@Injectable({
  providedIn: 'root'
})
export class AdminImpersonationStore {
  private router = inject(Router);
  private authStore = inject(AuthStore);
  private adminStore = inject(AdminStore);

  readonly isImpersonating = signal<boolean>(false);
  readonly targetWorkspace = signal<AdminWorkspaceSummary | null>(null);

  constructor() {
    this.checkSession();
  }

  checkSession(): void {
    try {
      const stored = sessionStorage.getItem(ADMIN_ROOT_SESSION_KEY);
      if (stored) {
        const payload = JSON.parse(stored);
        this.isImpersonating.set(true);
        this.targetWorkspace.set(payload.targetWorkspace || null);
      }
    } catch {
      // Storage unavailable
    }
  }

  startImpersonation(workspace: AdminWorkspaceSummary): void {
    const rootSnapshot = this.authStore.getSessionSnapshot();
    const backupPayload = {
      rootSession: rootSnapshot,
      targetWorkspace: workspace
    };

    try {
      sessionStorage.setItem(ADMIN_ROOT_SESSION_KEY, JSON.stringify(backupPayload));
    } catch {
      // Storage quota or error
    }

    // Set scoped session in AuthStore
    this.authStore.setSession({
      accessToken: `impersonation_token_${workspace.id}`,
      refreshToken: 'impersonation_refresh_token',
      user: {
        id: `owner_${workspace.id}`,
        fullName: workspace.ownerName,
        email: workspace.ownerEmail,
        phone: workspace.ownerPhone,
        initials: workspace.ownerName.slice(0, 2),
        isPlatformAdmin: false
      },
      activeWorkspace: {
        id: workspace.id,
        name: workspace.name,
        code: workspace.code,
        status: workspace.status,
        role: 'Owner',
        studentCount: workspace.studentCount,
        activeGroupCount: 5,
        subscriptionDaysRemaining: workspace.subscriptionDaysRemaining,
        subscriptionExpiresAt: workspace.subscriptionExpiresAt
      }
    });

    this.isImpersonating.set(true);
    this.targetWorkspace.set(workspace);

    this.adminStore.appendAuditLog({
      id: `aud_${Date.now()}`,
      createdAt: new Date().toISOString(),
      adminId: rootSnapshot.user?.id || 'usr_admin',
      adminName: rootSnapshot.user?.fullName || 'Super Admin',
      action: 'ADMIN_IMPERSONATION_STARTED',
      targetType: 'Workspace',
      targetId: workspace.id,
      targetName: workspace.name,
      afterState: { impersonatedWorkspace: workspace.name, owner: workspace.ownerName }
    });

    this.router.navigate(['/workspace/dashboard']);
  }

  exitImpersonation(): void {
    try {
      const stored = sessionStorage.getItem(ADMIN_ROOT_SESSION_KEY);
      if (stored) {
        const { rootSession, targetWorkspace } = JSON.parse(stored);
        if (rootSession) {
          this.authStore.setSession(rootSession);
        }
        sessionStorage.removeItem(ADMIN_ROOT_SESSION_KEY);

        this.adminStore.appendAuditLog({
          id: `aud_${Date.now()}`,
          createdAt: new Date().toISOString(),
          adminId: rootSession?.user?.id || 'usr_admin',
          adminName: rootSession?.user?.fullName || 'Super Admin',
          action: 'ADMIN_IMPERSONATION_ENDED',
          targetType: 'Workspace',
          targetId: targetWorkspace?.id || 'unknown',
          targetName: targetWorkspace?.name || 'unknown'
        });
      }
    } catch {
      // Storage error
    }

    this.isImpersonating.set(false);
    this.targetWorkspace.set(null);
    this.router.navigate(['/admin/accounts']);
  }
}
