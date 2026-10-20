/**
 * Core Barrel Exports
 * Milestone F2 & F3 Core Services, Stores, Enums, Models, Pipes, Interceptors & Guards
 */

// Enums
export * from './enums/domain.enums';

// Models
export * from './models/domain.models';
export * from './models/sync.models';
export * from './models/api-response.models';
export type {
  WorkspaceStatus,
  LockoutReason,
  WorkspaceSummary,
  AuthTokens,
  LoginRequest,
  LoginResponse,
  RefreshTokenRequest,
  RefreshTokenResponse,
  ReauthRequest
} from './models/auth.models';
export * from './models/route.models';
export * from './models/academic.models';

// Stores
export * from './stores/auth.store';

// Services
export * from './services/theme.service';
export * from './services/localization.service';
export * from './services/audio-feedback.service';
export * from './services/tahdeera-local-db';
export * from './services/offline-storage.service';
export * from './services/nfc-listener.service';
export { NfcWedgeAdapterService } from './services/nfc-wedge-adapter.service';
export * from './services/web-nfc-adapter.service';
export * from './services/identification-pipeline.service';
export * from './services/academic.service';
export * from './services/attendance-sync.service';

// Pipes
export * from './pipes/translate.pipe';

// Interceptors
export * from './interceptors/auth.interceptor';
export * from './interceptors/mock-api.interceptor';

// Guards
export * from './guards/auth.guard';
export * from './guards/role.guard';
export * from './guards/lockout.guard';
