/**
 * Auth & Language HTTP Interceptor
 * Milestone F2: Functional Interceptor injecting Bearer JWT, Accept-Language,
 * handling 401 silent token refresh, in-place re-auth modal trigger, and 403 lockout redirect.
 */

import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpEvent,
  HttpErrorResponse,
  HttpClient
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError, switchMap, catchError } from 'rxjs';
import { Router } from '@angular/router';
import { AuthStore } from '../stores/auth.store';
import { LocalizationService } from '../services/localization.service';
import { ReauthService } from '../services/reauth.service';
import { RefreshTokenResponse } from '../models/auth.models';

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const authStore = inject(AuthStore);
  const localization = inject(LocalizationService, { optional: true });
  const router = inject(Router, { optional: true });
  const reauthService = inject(ReauthService, { optional: true });
  const http = inject(HttpClient, { optional: true });

  const currentLang = localization ? localization.currentLang() : 'ar';
  const token = authStore.accessToken();

  const headers: Record<string, string> = {
    'Accept-Language': currentLang
  };

  if (token && !req.headers.has('Authorization')) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const clonedRequest = req.clone({
    setHeaders: headers
  });

  return next(clonedRequest).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      // 403 Lockout Interception (SUBSCRIPTION_EXPIRED / ADMIN_SUSPENDED)
      if (error.status === 403) {
        const errorDetail = error.error?.detail || error.error?.title || '';
        if (
          errorDetail === 'SUBSCRIPTION_EXPIRED' ||
          errorDetail.includes('SUBSCRIPTION_EXPIRED')
        ) {
          const currentWs = authStore.activeWorkspace();
          if (currentWs) {
            authStore.workspaces.update(list =>
              list.map(w => (w.id === currentWs.id ? { ...w, status: 'Expired' } : w))
            );
          }
          if (router) {
            router.navigate(['/workspace/lockout']);
          }
        } else if (
          errorDetail === 'ADMIN_SUSPENDED' ||
          errorDetail.includes('ADMIN_SUSPENDED')
        ) {
          const currentWs = authStore.activeWorkspace();
          if (currentWs) {
            authStore.workspaces.update(list =>
              list.map(w => (w.id === currentWs.id ? { ...w, status: 'Suspended' } : w))
            );
          }
          if (router) {
            router.navigate(['/workspace/lockout']);
          }
        }
        return throwError(() => error);
      }

      // 401 Silent Token Refresh & Re-Auth Handling
      if (error.status === 401) {
        const isAuthEndpoint =
          req.url.includes('/api/v1/auth/login') ||
          req.url.includes('/api/v1/auth/refresh') ||
          req.url.includes('/api/v1/auth/reauth');

        const refreshToken = authStore.refreshToken();

        if (!isAuthEndpoint && refreshToken && http) {
          return http
            .post<RefreshTokenResponse>('/api/v1/auth/refresh', { refreshToken })
            .pipe(
              switchMap(refreshRes => {
                authStore.updateTokens(refreshRes.tokens);
                const retryRequest = req.clone({
                  setHeaders: {
                    ...headers,
                    Authorization: `Bearer ${refreshRes.tokens.accessToken}`
                  }
                });
                return next(retryRequest);
              }),
              catchError((refreshErr: unknown) => {
                // Refresh failed: launch in-place re-auth modal
                const user = authStore.user();
                if (user && reauthService) {
                  reauthService.openModal(user.email);
                }
                return throwError(() => refreshErr);
              })
            );
        }
      }

      return throwError(() => error);
    })
  );
};
