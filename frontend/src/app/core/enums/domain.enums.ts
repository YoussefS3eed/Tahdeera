/**
 * Tahdeera Domain Enums
 * Authoritative types from data-model.md
 */

export enum WorkspaceRole {
  Owner = 'Owner',
  Admin = 'Admin',
  Assistant = 'Assistant',
  Teacher = 'Teacher',
  Viewer = 'Viewer'
}

export enum AttendanceStatus {
  Present = 'Present',
  Late = 'Late',
  Absent = 'Absent',
  Excused = 'Excused'
}

export enum ScanOutcome {
  Present = 'Present',
  Late = 'Late',
  Duplicate = 'Duplicate',
  Unregistered = 'Unregistered',
  Error = 'Error'
}

export enum SyncOperationStatus {
  PendingSync = 'PendingSync',
  Syncing = 'Syncing',
  Synced = 'Synced',
  Conflict = 'Conflict',
  Failed = 'Failed'
}

export enum SyncEntityType {
  Attendance = 'Attendance',
  Payment = 'Payment',
  Student = 'Student',
  Session = 'Session'
}

export enum SyncAction {
  Create = 'Create',
  Update = 'Update',
  Reverse = 'Reverse'
}

export enum ThemeMode {
  Light = 'light',
  Dark = 'dark',
  System = 'system'
}

export enum SupportedLanguage {
  Arabic = 'ar',
  English = 'en'
}
