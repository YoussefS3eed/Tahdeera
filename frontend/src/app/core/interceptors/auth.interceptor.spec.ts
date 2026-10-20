/**
 * Unit Tests for authInterceptor
 * Milestone F1 Phase 7: Header Injection Tests
 */

import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { authInterceptor } from './auth.interceptor';
import { LocalizationService } from '../services/localization.service';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        { provide: LocalizationService, useValue: { currentLang: () => 'ar' } },
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting()
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should append Authorization and Accept-Language headers', () => {
    httpClient.get('/api/v1/test').subscribe();

    const req = httpTestingController.expectOne('/api/v1/test');
    expect(req.request.headers.has('Authorization')).toBeTrue();
    expect(req.request.headers.get('Authorization')).toContain('Bearer ');
    expect(req.request.headers.has('Accept-Language')).toBeTrue();
    expect(req.request.headers.get('Accept-Language')).toBe('ar');

    req.flush({});
  });
});
