export type WorkspaceRole = 'Owner' | 'Admin' | 'Teacher' | 'Assistant' | 'Receptionist';
export type WorkspaceStatus = 'Active' | 'Expired' | 'Suspended';
export type LockoutReason = 'SubscriptionExpired' | 'AdminSuspended';

export interface UserProfile {
  id: string;
  fullName: string;
  email: string;
  phone: string;
  avatarUrl?: string | null;
  initials: string;
}

export interface WorkspaceSummary {
  id: string;
  name: string;
  code: string;
  status: WorkspaceStatus;
  role: WorkspaceRole;
  studentCount: number;
  activeGroupCount: number;
  subscriptionDaysRemaining: number;
  subscriptionExpiresAt: string; // ISO 8601 string
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  tokenType: 'Bearer';
  expiresIn: number;
}

export interface LoginRequest {
  identifier: string; // Email or Egyptian phone number (^01[0125][0-9]{8}$)
  password: string;
  rememberMe?: boolean;
}

export interface LoginResponse {
  user: UserProfile;
  tokens: AuthTokens;
  workspaces: WorkspaceSummary[];
  activeWorkspaceId: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  tokens: AuthTokens;
}

export interface ReauthRequest {
  password: string;
}
