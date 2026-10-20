import { WorkspaceRole } from './auth.models';

export interface AccessDeniedNotificationState {
  accessDenied: boolean;
  messageKey: string;
  attemptedUrl?: string;
  timestamp?: number;
}

export interface RouteRoleData {
  requiredRoles?: WorkspaceRole[];
  title?: string;
  tabKey?: string;
}
