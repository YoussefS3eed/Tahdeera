import { TestBed } from '@angular/core/testing';
import { AuthStore } from './auth.store';
import { LoginResponse } from '../models/auth.models';

describe('AuthStore', () => {
  let store: AuthStore;

  const mockTeacherLoginResponse: LoginResponse = {
    user: {
      id: 'usr_01',
      fullName: 'أ. محمد عبد الله',
      email: 'teacher@tahdeera.app',
      phone: '01012345670',
      initials: 'م.ع'
    },
    tokens: {
      accessToken: 'token_teacher_123',
      refreshToken: 'refresh_teacher_123',
      tokenType: 'Bearer',
      expiresIn: 900
    },
    workspaces: [
      {
        id: 'ws_01',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Active',
        role: 'Teacher',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 45,
        subscriptionExpiresAt: '2026-12-05T00:00:00Z'
      },
      {
        id: 'ws_02',
        name: 'سنتر النور',
        code: 'NOOR',
        status: 'Active',
        role: 'Teacher',
        studentCount: 110,
        activeGroupCount: 6,
        subscriptionDaysRemaining: 14,
        subscriptionExpiresAt: '2026-11-04T00:00:00Z'
      }
    ],
    activeWorkspaceId: 'ws_01'
  };

  const mockAssistantLoginResponse: LoginResponse = {
    user: {
      id: 'usr_02',
      fullName: 'أ. سارة أحمد',
      email: 'assistant@tahdeera.app',
      phone: '01112345670',
      initials: 'س.أ'
    },
    tokens: {
      accessToken: 'token_assistant_123',
      refreshToken: 'refresh_assistant_123',
      tokenType: 'Bearer',
      expiresIn: 900
    },
    workspaces: [
      {
        id: 'ws_01',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Active',
        role: 'Assistant',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 45,
        subscriptionExpiresAt: '2026-12-05T00:00:00Z'
      }
    ],
    activeWorkspaceId: 'ws_01'
  };

  const mockExpiredLoginResponse: LoginResponse = {
    user: {
      id: 'usr_03',
      fullName: 'أ. طارق محمود',
      email: 'expired@tahdeera.app',
      phone: '01212345670',
      initials: 'ط.م'
    },
    tokens: {
      accessToken: 'token_expired_123',
      refreshToken: 'refresh_expired_123',
      tokenType: 'Bearer',
      expiresIn: 900
    },
    workspaces: [
      {
        id: 'ws_expired',
        name: 'سنتر النجوم المنتهي',
        code: 'NOJOOM',
        status: 'Expired',
        role: 'Owner',
        studentCount: 95,
        activeGroupCount: 4,
        subscriptionDaysRemaining: 0,
        subscriptionExpiresAt: '2026-10-01T00:00:00Z'
      }
    ],
    activeWorkspaceId: 'ws_expired'
  };

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [AuthStore]
    });
    store = TestBed.inject(AuthStore);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should initialize with null unauthenticated state when localStorage is empty', () => {
    expect(store.isAuthenticated()).toBeFalse();
    expect(store.user()).toBeNull();
    expect(store.accessToken()).toBeNull();
    expect(store.activeWorkspace()).toBeNull();
    expect(store.isLockedOut()).toBeFalse();
    expect(store.canViewFinancials()).toBeFalse();
  });

  it('should authenticate and persist state on successful login', () => {
    store.login(mockTeacherLoginResponse);

    expect(store.isAuthenticated()).toBeTrue();
    expect(store.user()?.email).toBe('teacher@tahdeera.app');
    expect(store.accessToken()).toBe('token_teacher_123');
    expect(store.activeWorkspaceId()).toBe('ws_01');
    expect(store.currentUserRole()).toBe('Teacher');
    expect(store.canViewFinancials()).toBeFalse(); // Teacher is restricted from financials (Principle 9)
    expect(store.isLockedOut()).toBeFalse();

    expect(localStorage.getItem('tahdeera_auth_token')).toBe('token_teacher_123');
    expect(localStorage.getItem('tahdeera_refresh_token')).toBe('refresh_teacher_123');
  });

  it('should return true for canViewFinancials when role is Accountant or Owner (Principle 9)', () => {
    const mockAccountantLogin: LoginResponse = {
      ...mockTeacherLoginResponse,
      workspaces: [{ ...mockTeacherLoginResponse.workspaces[0], role: 'Accountant' }]
    };
    store.login(mockAccountantLogin);
    expect(store.currentUserRole()).toBe('Accountant');
    expect(store.canViewFinancials()).toBeTrue();

    const mockOwnerLogin: LoginResponse = {
      ...mockTeacherLoginResponse,
      workspaces: [{ ...mockTeacherLoginResponse.workspaces[0], role: 'Owner' }]
    };
    store.login(mockOwnerLogin);
    expect(store.currentUserRole()).toBe('Owner');
    expect(store.canViewFinancials()).toBeTrue();
  });

  it('should strictly return false for canViewFinancials when role is Assistant (CONF-09)', () => {
    store.login(mockAssistantLoginResponse);

    expect(store.currentUserRole()).toBe('Assistant');
    expect(store.canViewFinancials()).toBeFalse();
  });

  it('should correctly flag isLockedOut and lockoutReason for Expired workspaces', () => {
    store.login(mockExpiredLoginResponse);

    expect(store.isLockedOut()).toBeTrue();
    expect(store.lockoutReason()).toBe('SubscriptionExpired');
  });

  it('should switch active workspace and persist choice', () => {
    store.login(mockTeacherLoginResponse);
    expect(store.activeWorkspaceId()).toBe('ws_01');

    store.switchWorkspace('ws_02');
    expect(store.activeWorkspaceId()).toBe('ws_02');
    expect(store.activeWorkspace()?.name).toBe('سنتر النور');
    expect(localStorage.getItem('tahdeera_active_workspace')).toBe('ws_02');
  });

  it('should clear all session signals and storage on logout', () => {
    store.login(mockTeacherLoginResponse);
    expect(store.isAuthenticated()).toBeTrue();

    store.logout();

    expect(store.isAuthenticated()).toBeFalse();
    expect(store.user()).toBeNull();
    expect(store.accessToken()).toBeNull();
    expect(store.workspaces()).toEqual([]);
    expect(store.activeWorkspaceId()).toBeNull();
    expect(localStorage.getItem('tahdeera_auth_token')).toBeNull();
  });

  it('should restore session from localStorage on initSession', () => {
    localStorage.setItem('tahdeera_auth_token', 'restored_token');
    localStorage.setItem('tahdeera_refresh_token', 'restored_refresh');
    localStorage.setItem(
      'tahdeera_user_profile',
      JSON.stringify(mockTeacherLoginResponse.user)
    );
    localStorage.setItem(
      'tahdeera_workspaces_list',
      JSON.stringify(mockTeacherLoginResponse.workspaces)
    );
    localStorage.setItem('tahdeera_active_workspace', 'ws_02');

    store.initSession();

    expect(store.isAuthenticated()).toBeTrue();
    expect(store.user()?.fullName).toBe('أ. محمد عبد الله');
    expect(store.activeWorkspaceId()).toBe('ws_02');
    expect(store.activeWorkspace()?.code).toBe('NOOR');
  });
});
