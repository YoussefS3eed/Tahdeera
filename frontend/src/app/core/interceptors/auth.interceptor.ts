/**
 * Auth & Language HTTP Interceptor
 * Milestone F1 Phase 7: Functional Interceptor injecting Bearer JWT & Accept-Language
 */

import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';
import { LocalizationService } from '../services/localization.service';

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const localization = inject(LocalizationService, { optional: true });
  const currentLang = localization ? localization.currentLang() : 'ar';

  let token: string | null = null;
  if (typeof localStorage !== 'undefined') {
    token = localStorage.getItem('tahdeera_auth_token') || 'mock-jwt-token-teacher-001';
  } else {
    token = 'mock-jwt-token-teacher-001';
  }

  const headers: Record<string, string> = {
    'Accept-Language': currentLang
  };

  if (token && !req.headers.has('Authorization')) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const clonedRequest = req.clone({
    setHeaders: headers
  });

  return next(clonedRequest);
};
