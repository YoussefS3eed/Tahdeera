import { HttpResponse, HttpErrorResponse, HttpRequest } from '@angular/common/http';
import { LoginResponse, RefreshTokenResponse } from '../../models/auth.models';

export function handleAuthMockRequest(req: HttpRequest<unknown>): HttpResponse<unknown> | HttpErrorResponse | null {
  const url = req.url;
  const method = req.method;

  // 1. POST /api/v1/auth/login
  if (method === 'POST' && url.endsWith('/api/v1/auth/login')) {
    const body = req.body as { identifier?: string; password?: string };
    const id = (body?.identifier || '').trim().toLowerCase();
    const pass = (body?.password || '').trim();

    // Check credentials: Password123 or SecretPass123 accepted
    if (pass !== 'Password123' && pass !== 'SecretPass123') {
      return new HttpErrorResponse({
        status: 401,
        statusText: 'Unauthorized',
        url,
        error: {
          type: 'https://tools.ietf.org/html/rfc9457#section-3.1',
          title: 'Unauthorized',
          status: 401,
          detail: 'اسم المستخدم أو كلمة المرور غير صحيحة',
          instance: '/api/v1/auth/login'
        }
      });
    }

    // Role-specific demo presets
    if (id === 'assistant@tahdeera.app' || id === '01112345670') {
      const response: LoginResponse = {
        user: {
          id: 'usr_assistant_02',
          fullName: 'أ. سارة أحمد',
          email: 'assistant@tahdeera.app',
          phone: '01112345670',
          initials: 'س.أ'
        },
        tokens: {
          accessToken: 'mock_jwt_access_assistant_token',
          refreshToken: 'mock_jwt_refresh_assistant_token',
          tokenType: 'Bearer',
          expiresIn: 900
        },
        workspaces: [
          {
            id: 'ws_01',
            name: 'سنتر الأوائل - مدينة نصر',
            code: 'AWAEL-MN',
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
      return new HttpResponse({ status: 200, body: response });
    }

    if (id === 'receptionist@tahdeera.app' || id === '01212345670') {
      const response: LoginResponse = {
        user: {
          id: 'usr_receptionist_03',
          fullName: 'أ. كريم سامي',
          email: 'receptionist@tahdeera.app',
          phone: '01212345670',
          initials: 'ك.س'
        },
        tokens: {
          accessToken: 'mock_jwt_access_receptionist_token',
          refreshToken: 'mock_jwt_refresh_receptionist_token',
          tokenType: 'Bearer',
          expiresIn: 900
        },
        workspaces: [
          {
            id: 'ws_01',
            name: 'سنتر الأوائل - مدينة نصر',
            code: 'AWAEL-MN',
            status: 'Active',
            role: 'Receptionist',
            studentCount: 248,
            activeGroupCount: 12,
            subscriptionDaysRemaining: 45,
            subscriptionExpiresAt: '2026-12-05T00:00:00Z'
          }
        ],
        activeWorkspaceId: 'ws_01'
      };
      return new HttpResponse({ status: 200, body: response });
    }

    if (id === 'expired@tahdeera.app' || id === '01099999999') {
      const response: LoginResponse = {
        user: {
          id: 'usr_expired_04',
          fullName: 'أ. طارق محمود',
          email: 'expired@tahdeera.app',
          phone: '01099999999',
          initials: 'ط.م'
        },
        tokens: {
          accessToken: 'mock_jwt_access_expired_token',
          refreshToken: 'mock_jwt_refresh_expired_token',
          tokenType: 'Bearer',
          expiresIn: 900
        },
        workspaces: [
          {
            id: 'ws_expired',
            name: 'سنتر الأوائل (اشتراك منتهي)',
            code: 'AWAEL-EXP',
            status: 'Expired',
            role: 'Owner',
            studentCount: 248,
            activeGroupCount: 12,
            subscriptionDaysRemaining: 0,
            subscriptionExpiresAt: '2026-10-01T00:00:00Z'
          }
        ],
        activeWorkspaceId: 'ws_expired'
      };
      return new HttpResponse({ status: 200, body: response });
    }

    if (id === 'suspended@tahdeera.app' || id === '01088888888') {
      const response: LoginResponse = {
        user: {
          id: 'usr_suspended_05',
          fullName: 'أ. عادل فتحي',
          email: 'suspended@tahdeera.app',
          phone: '01088888888',
          initials: 'ع.ف'
        },
        tokens: {
          accessToken: 'mock_jwt_access_suspended_token',
          refreshToken: 'mock_jwt_refresh_suspended_token',
          tokenType: 'Bearer',
          expiresIn: 900
        },
        workspaces: [
          {
            id: 'ws_suspended',
            name: 'سنتر النور (موقوف إدارياً)',
            code: 'NOOR-SUSP',
            status: 'Suspended',
            role: 'Owner',
            studentCount: 180,
            activeGroupCount: 8,
            subscriptionDaysRemaining: 120,
            subscriptionExpiresAt: '2027-02-01T00:00:00Z'
          }
        ],
        activeWorkspaceId: 'ws_suspended'
      };
      return new HttpResponse({ status: 200, body: response });
    }

    // Default: Teacher (Owner)
    const defaultResponse: LoginResponse = {
      user: {
        id: 'usr_teacher_01',
        fullName: 'أ. محمد عبد الله',
        email: 'teacher@tahdeera.app',
        phone: '01012345670',
        initials: 'م.ع'
      },
      tokens: {
        accessToken: 'mock_jwt_access_teacher_token',
        refreshToken: 'mock_jwt_refresh_teacher_token',
        tokenType: 'Bearer',
        expiresIn: 900
      },
      workspaces: [
        {
          id: 'ws_01',
          name: 'سنتر الأوائل - مدينة نصر',
          code: 'AWAEL-MN',
          status: 'Active',
          role: 'Teacher',
          studentCount: 248,
          activeGroupCount: 12,
          subscriptionDaysRemaining: 45,
          subscriptionExpiresAt: '2026-12-05T00:00:00Z'
        },
        {
          id: 'ws_02',
          name: 'سنتر النور - المعادي',
          code: 'NOOR-MAADI',
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
    return new HttpResponse({ status: 200, body: defaultResponse });
  }

  // 2. POST /api/v1/auth/refresh
  if (method === 'POST' && url.endsWith('/api/v1/auth/refresh')) {
    const body = req.body as { refreshToken?: string };
    if (!body?.refreshToken || body.refreshToken === 'invalid') {
      return new HttpErrorResponse({
        status: 401,
        statusText: 'Unauthorized',
        url,
        error: {
          title: 'Unauthorized',
          detail: 'انتهت صلاحية جلسة العمل. يرجى إعادة إدخال كلمة المرور.'
        }
      });
    }

    const response: RefreshTokenResponse = {
      tokens: {
        accessToken: 'mock_jwt_access_refreshed_' + Date.now(),
        refreshToken: body.refreshToken,
        tokenType: 'Bearer',
        expiresIn: 900
      }
    };
    return new HttpResponse({ status: 200, body: response });
  }

  // 3. POST /api/v1/auth/reauth
  if (method === 'POST' && url.endsWith('/api/v1/auth/reauth')) {
    const body = req.body as { password?: string };
    if (body?.password !== 'Password123' && body?.password !== 'SecretPass123') {
      return new HttpErrorResponse({
        status: 401,
        statusText: 'Unauthorized',
        url,
        error: {
          title: 'Unauthorized',
          detail: 'كلمة المرور غير صحيحة'
        }
      });
    }

    const response = {
      success: true,
      tokens: {
        accessToken: 'mock_jwt_access_reauth_' + Date.now(),
        refreshToken: 'mock_jwt_refresh_reauth_' + Date.now(),
        tokenType: 'Bearer',
        expiresIn: 900
      }
    };
    return new HttpResponse({ status: 200, body: response });
  }

  // 4. POST /api/v1/auth/logout
  if (method === 'POST' && url.endsWith('/api/v1/auth/logout')) {
    return new HttpResponse({ status: 200, body: { success: true } });
  }

  return null;
}
