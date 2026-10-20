import { Injectable, computed, signal } from '@angular/core';
import {
  AuthTokens,
  LockoutReason,
  LoginResponse,
  UserProfile,
  WorkspaceRole,
  WorkspaceSummary
} from '../models/auth.models';

const STORAGE_KEY_AUTH_TOKEN = 'tahdeera_auth_token';
const STORAGE_KEY_REFRESH_TOKEN = 'tahdeera_refresh_token';
const STORAGE_KEY_ACTIVE_WORKSPACE = 'tahdeera_active_workspace';
const STORAGE_KEY_USER_PROFILE = 'tahdeera_user_profile';
const STORAGE_KEY_WORKSPACES = 'tahdeera_workspaces_list';

@Injectable({
  providedIn: 'root'
})
export class AuthStore {
  readonly user = signal<UserProfile | null>(null);
  readonly accessToken = signal<string | null>(null);
  readonly refreshToken = signal<string | null>(null);
  readonly workspaces = signal<WorkspaceSummary[]>([]);
  readonly activeWorkspaceId = signal<string | null>(null);

  readonly isAuthenticated = computed(() => !!this.accessToken() && !!this.user());

  readonly activeWorkspace = computed<WorkspaceSummary | null>(() => {
    const list = this.workspaces();
    const id = this.activeWorkspaceId();
    if (!id || list.length === 0) return null;
    return list.find(w => w.id === id) ?? null;
  });

  readonly currentUserRole = computed<WorkspaceRole | null>(() => {
    return this.activeWorkspace()?.role ?? null;
  });

  readonly isLockedOut = computed<boolean>(() => {
    const ws = this.activeWorkspace();
    return ws?.status === 'Expired' || ws?.status === 'Suspended';
  });

  readonly lockoutReason = computed<LockoutReason | null>(() => {
    const ws = this.activeWorkspace();
    if (ws?.status === 'Expired') return 'SubscriptionExpired';
    if (ws?.status === 'Suspended') return 'AdminSuspended';
    return null;
  });

  readonly canViewFinancials = computed<boolean>(() => {
    const role = this.currentUserRole();
    if (!role) return false;
    return role === 'Owner' || role === 'Admin' || role === 'Teacher';
  });

  constructor() {
    this.initSession();
  }

  initSession(): void {
    try {
      const storedToken = localStorage.getItem(STORAGE_KEY_AUTH_TOKEN);
      const storedRefreshToken = localStorage.getItem(STORAGE_KEY_REFRESH_TOKEN);
      const storedUser = localStorage.getItem(STORAGE_KEY_USER_PROFILE);
      const storedWorkspaces = localStorage.getItem(STORAGE_KEY_WORKSPACES);

      if (storedToken && storedUser) {
        this.accessToken.set(storedToken);
        this.refreshToken.set(storedRefreshToken ?? null);
        this.user.set(JSON.parse(storedUser));

        if (storedWorkspaces) {
          const parsedWorkspaces: WorkspaceSummary[] = JSON.parse(storedWorkspaces);
          this.workspaces.set(parsedWorkspaces);
          this.resolveAndSetActiveWorkspace(parsedWorkspaces);
        }
      }
    } catch {
      // LocalStorage unavailable or parse error — graceful fallback
    }
  }

  login(response: LoginResponse): void {
    this.user.set(response.user);
    this.accessToken.set(response.tokens.accessToken);
    this.refreshToken.set(response.tokens.refreshToken);
    this.workspaces.set(response.workspaces);

    try {
      localStorage.setItem(STORAGE_KEY_AUTH_TOKEN, response.tokens.accessToken);
      localStorage.setItem(STORAGE_KEY_REFRESH_TOKEN, response.tokens.refreshToken);
      localStorage.setItem(STORAGE_KEY_USER_PROFILE, JSON.stringify(response.user));
      localStorage.setItem(STORAGE_KEY_WORKSPACES, JSON.stringify(response.workspaces));
    } catch {
      // Storage quota or restriction fallback
    }

    this.resolveAndSetActiveWorkspace(response.workspaces, response.activeWorkspaceId);
  }

  switchWorkspace(workspaceId: string): void {
    const found = this.workspaces().find(w => w.id === workspaceId);
    if (found) {
      this.activeWorkspaceId.set(workspaceId);
      try {
        localStorage.setItem(STORAGE_KEY_ACTIVE_WORKSPACE, workspaceId);
      } catch {
        // Ignored
      }
    }
  }

  updateTokens(tokens: AuthTokens): void {
    this.accessToken.set(tokens.accessToken);
    this.refreshToken.set(tokens.refreshToken);
    try {
      localStorage.setItem(STORAGE_KEY_AUTH_TOKEN, tokens.accessToken);
      localStorage.setItem(STORAGE_KEY_REFRESH_TOKEN, tokens.refreshToken);
    } catch {
      // Ignored
    }
  }

  logout(): void {
    this.user.set(null);
    this.accessToken.set(null);
    this.refreshToken.set(null);
    this.workspaces.set([]);
    this.activeWorkspaceId.set(null);

    try {
      localStorage.removeItem(STORAGE_KEY_AUTH_TOKEN);
      localStorage.removeItem(STORAGE_KEY_REFRESH_TOKEN);
      localStorage.removeItem(STORAGE_KEY_USER_PROFILE);
      localStorage.removeItem(STORAGE_KEY_WORKSPACES);
      // Retain or clear active workspace
      localStorage.removeItem(STORAGE_KEY_ACTIVE_WORKSPACE);
    } catch {
      // Ignored
    }
  }

  /**
   * Deterministic 4-Step Hydration Algorithm:
   * 1. Check localStorage ('tahdeera_active_workspace')
   * 2. If present in user.workspaces -> select it
   * 3. Else find first workspace with status === 'Active'
   * 4. Else fall back to workspaces[0]
   */
  private resolveAndSetActiveWorkspace(
    workspaces: WorkspaceSummary[],
    hintId?: string
  ): void {
    if (!workspaces || workspaces.length === 0) {
      this.activeWorkspaceId.set(null);
      return;
    }

    let savedId: string | null = null;
    try {
      savedId = localStorage.getItem(STORAGE_KEY_ACTIVE_WORKSPACE);
    } catch {
      savedId = null;
    }

    // Step 1 & 2: Match saved ID or hint
    const targetId = savedId || hintId;
    if (targetId && workspaces.some(w => w.id === targetId)) {
      this.activeWorkspaceId.set(targetId);
      return;
    }

    // Step 3: First Active workspace
    const firstActive = workspaces.find(w => w.status === 'Active');
    if (firstActive) {
      this.activeWorkspaceId.set(firstActive.id);
      try {
        localStorage.setItem(STORAGE_KEY_ACTIVE_WORKSPACE, firstActive.id);
      } catch {
        // Ignored
      }
      return;
    }

    // Step 4: Fallback to first workspace in list
    const fallback = workspaces[0];
    this.activeWorkspaceId.set(fallback.id);
    try {
      localStorage.setItem(STORAGE_KEY_ACTIVE_WORKSPACE, fallback.id);
    } catch {
      // Ignored
    }
  }
}
