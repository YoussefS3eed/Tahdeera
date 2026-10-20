/**
 * Unit Tests for Application Routing
 * Milestone F2 / User Story 3: Application Routing Integration Tests
 */

import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideLocationMocks } from '@angular/common/testing';
import { Location } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { routes } from './app.routes';

describe('App Routes', () => {
  let router: Router;
  let location: Location;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideLocationMocks(),
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    location = TestBed.inject(Location);
  });

  it('should navigate to root / successfully', async () => {
    await router.navigateByUrl('/');
    expect(['', '/']).toContain(location.path());
  });

  it('should navigate to /login successfully', async () => {
    await router.navigateByUrl('/login');
    expect(location.path()).toBe('/login');
  });

  it('should navigate to /workspace successfully', async () => {
    await router.navigateByUrl('/workspace');
    expect(location.path()).toBe('/workspace');
  });

  it('should navigate to /workspace/dashboard successfully', async () => {
    await router.navigateByUrl('/workspace/dashboard');
    expect(location.path()).toBe('/workspace/dashboard');
  });

  it('should navigate to all unimplemented workspace child routes', async () => {
    const childPaths = [
      '/workspace/nfc-station',
      '/workspace/attendance',
      '/workspace/students',
      '/workspace/cards',
      '/workspace/groups',
      '/workspace/schedule',
      '/workspace/payments',
      '/workspace/whatsapp',
      '/workspace/lms',
      '/workspace/reports',
      '/workspace/team',
      '/workspace/settings',
      '/workspace/ai-assistant'
    ];

    for (const path of childPaths) {
      await router.navigateByUrl(path);
      expect(location.path()).toBe(path);
    }
  });

  it('should redirect unknown wildcard routes to root /', async () => {
    await router.navigateByUrl('/unknown-route-12345');
    expect(['', '/']).toContain(location.path());
  });
});
