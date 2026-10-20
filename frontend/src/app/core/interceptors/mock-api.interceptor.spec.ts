/**
 * Unit Tests for mockApiInterceptor
 * Milestone F1 Phase 7: Mock API Latency, Pagination & RFC 9457 Problem Details
 */

import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors, HttpHeaders } from '@angular/common/http';
import { mockApiInterceptor } from './mock-api.interceptor';
import { PagedResult, ProblemDetails } from '../models/api-response.models';
import { Student } from '../models/domain.models';

describe('mockApiInterceptor', () => {
  let httpClient: HttpClient;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([mockApiInterceptor]))
      ]
    });

    httpClient = TestBed.inject(HttpClient);
  });

  it('should return paginated students with PagedResult structure', (done) => {
    const headers = new HttpHeaders({ 'X-Mock-Delay': '0' });
    httpClient.get<PagedResult<Student>>('/api/v1/students?pageNumber=1&pageSize=10', { headers }).subscribe({
      next: (res) => {
        expect(res).toBeTruthy();
        expect(res.items.length).toBe(10);
        expect(res.totalCount).toBe(15);
        expect(res.pageNumber).toBe(1);
        expect(res.totalPages).toBe(2);
        expect(res.hasNextPage).toBeTrue();
        done();
      },
      error: done.fail
    });
  });

  it('should return page 2 with remaining 5 students', (done) => {
    const headers = new HttpHeaders({ 'X-Mock-Delay': '0' });
    httpClient.get<PagedResult<Student>>('/api/v1/students?pageNumber=2&pageSize=10', { headers }).subscribe({
      next: (res) => {
        expect(res.items.length).toBe(5);
        expect(res.pageNumber).toBe(2);
        expect(res.hasNextPage).toBeFalse();
        expect(res.hasPreviousPage).toBeTrue();
        done();
      },
      error: done.fail
    });
  });

  it('should return HTTP 409 Conflict with RFC 9457 ProblemDetails on conflict simulation', (done) => {
    const headers = new HttpHeaders({ 'X-Mock-Delay': '0' });
    httpClient.post('/api/v1/attendance?simulateConflict=true', {
      sessionId: 'sess-001',
      studentId: 'st-001'
    }, { headers }).subscribe({
      next: () => done.fail('Should have failed with 409'),
      error: (err) => {
        expect(err.status).toBe(409);
        const body: ProblemDetails = err.error;
        expect(body.title).toBe('Conflict');
        expect(body.errorCode).toBe('DUPLICATE_CHECKIN');
        expect(body.instance).toBe('/api/v1/attendance');
        done();
      }
    });
  });

  it('should return simulated error for requested status codes', (done) => {
    const headers = new HttpHeaders({ 'X-Mock-Delay': '0', 'X-Simulate-Error': '403' });
    httpClient.get('/api/v1/test', { headers }).subscribe({
      next: () => done.fail('Should have failed with 403'),
      error: (err) => {
        expect(err.status).toBe(403);
        const body: ProblemDetails = err.error;
        expect(body.errorCode).toBe('INSUFFICIENT_PERMISSIONS');
        done();
      }
    });
  });
});
