/**
 * Mock API Interceptor
 * Milestone F1 Phase 7: Constitution Principle 6 Standalone Mock API Layer with RFC 9457 Problem Details
 */

import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpEvent,
  HttpResponse,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { delay } from 'rxjs/operators';
import { Student, ClassSession, AttendanceRecord } from '../models/domain.models';
import { PagedResult, ProblemDetails } from '../models/api-response.models';
import { AttendanceStatus, SyncOperationStatus } from '../enums/domain.enums';
import { handlePublicInquiry } from '../mock/handlers/public.mock';
import { handleAuthMockRequest } from '../mock/handlers/auth.mock';
import { handleDashboardMockRequest } from '../mock/handlers/dashboard.mock';
import { handleAcademicMockRequest } from '../mocks/academic-mock.interceptor';
import { handleF4MockRequest } from '../mocks/f4-mock.interceptor';
import { handleAdminMockRequest } from '../mocks/admin-mock.interceptor';

// In-Memory Seed Data Fixture
const MOCK_STUDENTS: Student[] = [
  { id: 'st-001', workspaceId: 'ws-cairo-01', studentCode: 'ST-1001', fullName: 'يوسف محمد علي', guardianPhone: '01012345671', nfcCardUid: '04A27C19', currentBalance: 0, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-002', workspaceId: 'ws-cairo-01', studentCode: 'ST-1002', fullName: 'أحمد محمود حسن', guardianPhone: '01012345672', nfcCardUid: '04A27C20', currentBalance: 50, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-003', workspaceId: 'ws-cairo-01', studentCode: 'ST-1003', fullName: 'مريم طارق سامي', guardianPhone: '01112345673', nfcCardUid: '04A27C21', currentBalance: 0, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-004', workspaceId: 'ws-cairo-01', studentCode: 'ST-1004', fullName: 'عمر خالد إبراهيم', guardianPhone: '01212345674', nfcCardUid: '04A27C22', currentBalance: -100, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-005', workspaceId: 'ws-cairo-01', studentCode: 'ST-1005', fullName: 'سلمى شريف نبيل', guardianPhone: '01512345675', nfcCardUid: '04A27C23', currentBalance: 0, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-006', workspaceId: 'ws-cairo-01', studentCode: 'ST-1006', fullName: 'كريم وائل سمير', guardianPhone: '01012345676', nfcCardUid: '04A27C24', currentBalance: 0, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-007', workspaceId: 'ws-cairo-01', studentCode: 'ST-1007', fullName: 'نور الدين ياسر', guardianPhone: '01112345677', nfcCardUid: '04A27C25', currentBalance: 0, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-008', workspaceId: 'ws-cairo-01', studentCode: 'ST-1008', fullName: 'حبيبة علاء الدين', guardianPhone: '01212345678', nfcCardUid: '04A27C26', currentBalance: 0, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-009', workspaceId: 'ws-cairo-01', studentCode: 'ST-1009', fullName: 'مصطفى أشرف كمال', guardianPhone: '01512345679', nfcCardUid: '04A27C27', currentBalance: 120, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-010', workspaceId: 'ws-cairo-01', studentCode: 'ST-1010', fullName: 'هنا أحمد فهمي', guardianPhone: '01012345680', nfcCardUid: '04A27C28', currentBalance: 0, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-011', workspaceId: 'ws-cairo-01', studentCode: 'ST-1011', fullName: 'إبراهيم صبري فؤاد', guardianPhone: '01112345681', nfcCardUid: '04A27C29', currentBalance: 0, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-012', workspaceId: 'ws-cairo-01', studentCode: 'ST-1012', fullName: 'ملك هاني عبد الله', guardianPhone: '01212345682', nfcCardUid: '04A27C30', currentBalance: 0, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-013', workspaceId: 'ws-cairo-01', studentCode: 'ST-1013', fullName: 'حمزة إيهاب رشدي', guardianPhone: '01512345683', nfcCardUid: '04A27C31', currentBalance: 0, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-014', workspaceId: 'ws-cairo-01', studentCode: 'ST-1014', fullName: 'جنى ماجد عثمان', guardianPhone: '01012345684', nfcCardUid: '04A27C32', currentBalance: 0, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' },
  { id: 'st-015', workspaceId: 'ws-cairo-01', studentCode: 'ST-1015', fullName: 'زياد شريف مروان', guardianPhone: '01112345685', nfcCardUid: '04A27C33', currentBalance: 0, isActive: true, createdAtUtc: '2026-10-01T08:00:00Z' }
];

const MOCK_SESSIONS: ClassSession[] = [
  {
    id: 'sess-001',
    workspaceId: 'ws-cairo-01',
    groupId: 'grp-chem-1',
    groupName: 'مجموعة النخبة أ',
    subjectName: 'كيمياء الثانوية العامة',
    startsAt: new Date(Date.now() - 30 * 60 * 1000).toISOString(),
    endsAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    status: 'Active'
  },
  {
    id: 'sess-002',
    workspaceId: 'ws-cairo-01',
    groupId: 'grp-phys-1',
    groupName: 'مجموعة المتفوقين ب',
    subjectName: 'فيزياء لغات',
    startsAt: new Date(Date.now() - 25 * 60 * 60 * 1000).toISOString(),
    endsAt: new Date(Date.now() - 23 * 60 * 60 * 1000).toISOString(),
    status: 'Completed'
  }
];

export const mockApiInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const url = req.url;

  // Intercept only /api/v1/ endpoints
  if (!url.startsWith('/api/v1/')) {
    return next(req);
  }

  // Latency calculation: bypass to 0ms if X-Mock-Delay is 0
  let delayMs = Math.floor(Math.random() * 100) + 150; // 150-250ms
  if (req.headers.has('X-Mock-Delay')) {
    const override = parseInt(req.headers.get('X-Mock-Delay') || '0', 10);
    delayMs = isNaN(override) ? 0 : override;
  }

  // Helper to extract param from HttpRequest params or URL search string
  const getParam = (name: string): string | null => {
    if (req.params.has(name)) return req.params.get(name);
    try {
      const qIdx = url.indexOf('?');
      if (qIdx !== -1) {
        const searchParams = new URLSearchParams(url.substring(qIdx));
        if (searchParams.has(name)) return searchParams.get(name);
      }
    } catch {
      // Fallback
    }
    return null;
  };

  // Simulated Error Triggers (via header or query param)
  const simulateErrorHeader = req.headers.get('X-Simulate-Error');
  const simulateErrorParam = getParam('simulateError');
  if (simulateErrorHeader || simulateErrorParam) {
    const status = parseInt(simulateErrorHeader || simulateErrorParam || '500', 10);
    const problem = createProblemDetails(status, url);
    return throwError(() => new HttpErrorResponse({
      status,
      statusText: problem.title,
      error: problem,
      url: req.url
    })).pipe(delayIf(delayMs));
  }

  // ==========================================
  // Endpoint Routing
  // ==========================================

  // Milestone F3 Academic Operations (SCR-05 to SCR-11)
  const academicRes$ = handleAcademicMockRequest(req);
  if (academicRes$) {
    return academicRes$;
  }

  // Milestone F4 Operations (SCR-12 to SCR-18)
  const f4Res$ = handleF4MockRequest(req);
  if (f4Res$) {
    return f4Res$;
  }

  // Milestone F5 Operations (SCR-19 to SCR-26)
  const adminRes$ = handleAdminMockRequest(req);
  if (adminRes$) {
    return adminRes$;
  }

  // 1. GET /api/v1/students
  if (req.method === 'GET' && url.includes('/api/v1/students')) {
    const pageNumber = Math.max(1, parseInt(getParam('pageNumber') || '1', 10));
    const pageSize = Math.max(1, parseInt(getParam('pageSize') || '10', 10));
    const search = (getParam('search') || '').trim().toLowerCase();

    let filtered = MOCK_STUDENTS;
    if (search) {
      filtered = filtered.filter(s =>
        s.fullName.toLowerCase().includes(search) ||
        s.studentCode.toLowerCase().includes(search) ||
        s.guardianPhone.includes(search)
      );
    }

    const totalCount = filtered.length;
    const totalPages = Math.ceil(totalCount / pageSize);
    const startIndex = (pageNumber - 1) * pageSize;
    const items = filtered.slice(startIndex, startIndex + pageSize);

    const result: PagedResult<Student> = {
      items,
      totalCount,
      pageNumber,
      pageSize,
      totalPages,
      hasPreviousPage: pageNumber > 1,
      hasNextPage: pageNumber < totalPages
    };

    return of(new HttpResponse({ status: 200, body: result })).pipe(delayIf(delayMs));
  }

  // 2. GET /api/v1/sessions
  if (req.method === 'GET' && url.includes('/api/v1/sessions')) {
    const result: PagedResult<ClassSession> = {
      items: MOCK_SESSIONS,
      totalCount: MOCK_SESSIONS.length,
      pageNumber: 1,
      pageSize: 10,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false
    };
    return of(new HttpResponse({ status: 200, body: result })).pipe(delayIf(delayMs));
  }

  // 3. POST /api/v1/attendance
  if (req.method === 'POST' && url.includes('/api/v1/attendance')) {
    const body = req.body as Record<string, string> || {};

    // Simulate 409 Conflict if query param simulateConflict=true or studentId is already checked in
    const isConflict = getParam('simulateConflict') === 'true' || body['simulateConflict'];
    if (isConflict) {
      const errorBody: ProblemDetails = {
        type: 'https://errors.tahdeera.com/attendance/duplicate-checkin',
        title: 'Conflict',
        status: 409,
        detail: 'الطالب مسجل حضور بالفعل في هذه الجلسة',
        instance: '/api/v1/attendance',
        errorCode: 'DUPLICATE_CHECKIN',
        traceId: `00-${Date.now()}-01`,
        errors: {
          studentId: ['Student already checked in for this session at 09:15 UTC']
        }
      };

      return throwError(() => new HttpErrorResponse({
        status: 409,
        statusText: 'Conflict',
        error: errorBody,
        url: req.url
      })).pipe(delayIf(delayMs));
    }

    const newAttendance: AttendanceRecord = {
      id: 'att-' + Date.now(),
      sessionId: body['sessionId'] || 'sess-001',
      studentId: body['studentId'] || 'st-001',
      studentName: 'يوسف محمد علي',
      studentCode: 'ST-1001',
      status: AttendanceStatus.Present,
      markedAtUtc: body['scannedAtUtc'] || new Date().toISOString(),
      isAutomaticEvaluation: true,
      isLocked: false
    };

    return of(new HttpResponse({ status: 201, body: newAttendance })).pipe(delayIf(delayMs));
  }

  if (req.method === 'POST' && url.includes('/api/v1/sync/batch')) {
    const operations = Array.isArray(req.body) ? req.body : [];
    const syncedIds = operations.map(o => o.localId || 1);
    return of(new HttpResponse({
      status: 200,
      body: { syncedIds, conflicts: [], failedIds: [] }
    })).pipe(delayIf(delayMs));
  }

  if (req.method === 'GET' && url.includes('/api/v1/auth/me')) {
    const me = {
      id: 'usr-001',
      username: 'ahmed.fathi',
      fullName: 'أ/ أحمد فتحي',
      role: 'Teacher',
      workspaceId: 'ws-cairo-01',
      preferredLanguage: 'ar',
      preferredTheme: 'light'
    };
    return of(new HttpResponse({ status: 200, body: me })).pipe(delayIf(delayMs));
  }

  // 4. POST /api/v1/public/inquiries
  if (req.method === 'POST' && url.includes('/api/v1/public/inquiries')) {
    return handlePublicInquiry(req).pipe(delayIf(delayMs));
  }

  // 5. Auth Endpoints: /api/v1/auth/*
  if (url.includes('/api/v1/auth/')) {
    const authRes = handleAuthMockRequest(req);
    if (authRes instanceof HttpErrorResponse) {
      return throwError(() => authRes).pipe(delayIf(delayMs));
    }
    if (authRes instanceof HttpResponse) {
      return of(authRes).pipe(delayIf(delayMs));
    }
  }

  // 6. Dashboard Overview: /api/v1/workspaces/:id/dashboard
  if (url.includes('/dashboard')) {
    const dashRes = handleDashboardMockRequest(req);
    if (dashRes instanceof HttpErrorResponse) {
      return throwError(() => dashRes).pipe(delayIf(delayMs));
    }
    if (dashRes instanceof HttpResponse) {
      return of(dashRes).pipe(delayIf(delayMs));
    }
  }

  // Fallback 404
  const notFound = createProblemDetails(404, url);
  return throwError(() => new HttpErrorResponse({
    status: 404,
    statusText: 'Not Found',
    error: notFound,
    url: req.url
  })).pipe(delayIf(delayMs));
};

function delayIf<T>(ms: number) {
  return (source: Observable<T>): Observable<T> => {
    return ms > 0 ? source.pipe(delay(ms)) : source;
  };
}

function createProblemDetails(status: number, url: string): ProblemDetails {
  const titles: Record<number, string> = {
    400: 'Bad Request',
    401: 'Unauthorized',
    403: 'Forbidden',
    404: 'Not Found',
    409: 'Conflict',
    422: 'Unprocessable Entity',
    500: 'Internal Server Error'
  };

  const errorCodes: Record<number, string> = {
    400: 'BAD_REQUEST',
    401: 'UNAUTHORIZED_ACCESS',
    403: 'INSUFFICIENT_PERMISSIONS',
    404: 'RESOURCE_NOT_FOUND',
    409: 'CONFLICT_STATE',
    422: 'VALIDATION_FAILED',
    500: 'INTERNAL_SERVER_ERROR'
  };

  return {
    type: `https://errors.tahdeera.com/status/${status}`,
    title: titles[status] || 'Error',
    status,
    detail: `Simulated error ${status} on endpoint ${url}`,
    instance: url,
    errorCode: errorCodes[status] || 'UNKNOWN_ERROR',
    traceId: `00-${Math.random().toString(16).substring(2, 18)}-01`
  };
}
