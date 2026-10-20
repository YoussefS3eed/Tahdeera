/**
 * Unit Tests for authInterceptor
 * Milestone F2: Header Injection, 401 Silent Refresh, and 403 Lockout Tests
 */

import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { LocalizationService } from '../services/localization.service';
import { AuthStore } from '../stores/auth.store';
import { ReauthService } from '../services/reauth.service';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpTestingController: HttpTestingController;
  let authStore: AuthStore;
  let reauthService: ReauthService;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    localStorage.clear();
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        AuthStore,
        ReauthService,
        { provide: Router, useValue: router },
        { provide: LocalizationService, useValue: { currentLang: () => 'ar' } },
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting()
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    httpTestingController = TestBed.inject(HttpTestingController);
    authStore = TestBed.inject(AuthStore);
    reauthService = TestBed.inject(ReauthService);

    authStore.accessToken.set('valid_mock_access_token');
    authStore.refreshToken.set('valid_mock_refresh_token');
    authStore.user.set({
      id: 'usr_01',
      fullName: 'أ. محمد',
      email: 'teacher@tahdeera.app',
      phone: '01012345670',
      initials: 'م.ع'
    });
    authStore.workspaces.set([
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
      }
    ]);
    authStore.activeWorkspaceId.set('ws_01');
  });

  afterEach(() => {
    httpTestingController.verify();
    localStorage.clear();
  });

  it('should append Authorization and Accept-Language headers', () => {
    httpClient.get('/api/v1/students').subscribe();

    const req = httpTestingController.expectOne('/api/v1/students');
    expect(req.request.headers.has('Authorization')).toBeTrue();
    expect(req.request.headers.get('Authorization')).toBe('Bearer valid_mock_access_token');
    expect(req.request.headers.has('Accept-Language')).toBeTrue();
    expect(req.request.headers.get('Accept-Language')).toBe('ar');

    req.flush([]);
  });

  it('should redirect to /workspace/lockout when HTTP 403 SUBSCRIPTION_EXPIRED occurs', () => {
    httpClient.get('/api/v1/workspaces/ws_01/dashboard').subscribe({
      next: () => fail('Should have failed with 403'),
      error: err => {
        expect(err.status).toBe(403);
      }
    });

    const req = httpTestingController.expectOne('/api/v1/workspaces/ws_01/dashboard');
    req.flush(
      { detail: 'SUBSCRIPTION_EXPIRED' },
      { status: 403, statusText: 'Forbidden' }
    );

    expect(router.navigate).toHaveBeenCalledWith(['/workspace/lockout']);
    expect(authStore.activeWorkspace()?.status).toBe('Expired');
  });

  it('should attempt silent token refresh and retry request on HTTP 401', () => {
    httpClient.get('/api/v1/data').subscribe(res => {
      expect(res).toEqual({ success: true });
    });

    // Initial request fails with 401
    const initialReq = httpTestingController.expectOne('/api/v1/data');
    initialReq.flush(
      { detail: 'Token Expired' },
      { status: 401, statusText: 'Unauthorized' }
    );

    // Refresh request triggered
    const refreshReq = httpTestingController.expectOne('/api/v1/auth/refresh');
    expect(refreshReq.request.body).toEqual({ refreshToken: 'valid_mock_refresh_token' });

    // Refresh succeeds with new tokens
    refreshReq.flush({
      tokens: {
        accessToken: 'fresh_access_token',
        refreshToken: 'fresh_refresh_token',
        tokenType: 'Bearer',
        expiresIn: 900
      }
    });

    // Retried request with fresh token
    const retryReq = httpTestingController.expectOne('/api/v1/data');
    expect(retryReq.request.headers.get('Authorization')).toBe('Bearer fresh_access_token');
    retryReq.flush({ success: true });
  });

  it('should open in-place re-auth modal when silent refresh fails', () => {
    spyOn(reauthService, 'openModal');

    httpClient.get('/api/v1/protected').subscribe({
      next: () => fail('Should have failed'),
      error: () => {}
    });

    const initialReq = httpTestingController.expectOne('/api/v1/protected');
    initialReq.flush({}, { status: 401, statusText: 'Unauthorized' });

    const refreshReq = httpTestingController.expectOne('/api/v1/auth/refresh');
    refreshReq.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(reauthService.openModal).toHaveBeenCalledWith('teacher@tahdeera.app');
  });
});
