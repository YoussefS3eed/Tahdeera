/**
 * Academic Mock HTTP Interceptor & Handlers
 * Simulates REST APIs for Milestone F3 Academic Operations with realistic state mutations,
 * RFC 9457 ProblemDetails, and domain invariants (Egyptian phone regex, 10-char retroactive gate).
 * Reference: specs/010-f3-students-nfc-attendance/contracts/
 */

import { HttpRequest, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { delay } from 'rxjs/operators';
import {
  MOCK_STUDENTS,
  MOCK_STUDENT_360,
  MOCK_STUDY_GROUPS,
  MOCK_CLASS_SESSIONS,
  MOCK_ATTENDANCE_RECORDS,
  MOCK_NFC_CARDS
} from './academic-mock-data';
import {
  StudentListItem,
  Student360Profile,
  StudyGroupItem,
  ClassSessionItem,
  AttendanceRecordItem,
  NfcCardItem,
  CredentialScanResult,
  ACADEMIC_VALIDATION_REGEX
} from '../models/academic.models';

// Stateful mock in-memory stores
let studentsStore: StudentListItem[] = [...MOCK_STUDENTS];
let groupsStore: StudyGroupItem[] = [...MOCK_STUDY_GROUPS];
let sessionsStore: ClassSessionItem[] = [...MOCK_CLASS_SESSIONS];
let attendanceStore: AttendanceRecordItem[] = [...MOCK_ATTENDANCE_RECORDS];
let cardsStore: NfcCardItem[] = [...MOCK_NFC_CARDS];

function createProblem(status: number, title: string, detail: string, errorCode: string, url: string): HttpErrorResponse {
  return new HttpErrorResponse({
    status,
    statusText: title,
    error: {
      type: `https://errors.tahdeera.com/academic/${errorCode.toLowerCase()}`,
      title,
      status,
      detail,
      instance: url,
      errorCode,
      traceId: `00-${Math.random().toString(16).substring(2, 18)}-01`
    }
  });
}

export function handleAcademicMockRequest(req: HttpRequest<any>): Observable<any> | null {
  const url = req.url;
  const method = req.method;

  // 1. GET /api/v1/students
  if (url.startsWith('/api/v1/students') && method === 'GET') {
    const urlObj = new URL(url, 'http://localhost');
    if (urlObj.searchParams.has('pageNumber')) {
      return null; // Fall through to original paginated mockApiInterceptor handler
    }
    const search = urlObj.searchParams.get('search')?.toLowerCase();
    const grade = urlObj.searchParams.get('grade');
    const status = urlObj.searchParams.get('status');

    let filtered = [...studentsStore];
    if (search) {
      filtered = filtered.filter(s =>
        s.fullName.toLowerCase().includes(search) ||
        s.studentCode.toLowerCase().includes(search) ||
        s.guardianPhone.includes(search)
      );
    }
    if (grade && grade !== 'All') {
      filtered = filtered.filter(s => s.grade === grade);
    }
    if (status && status !== 'All') {
      filtered = filtered.filter(s => s.status === status);
    }

    // Single student by ID check (/api/v1/students/std-XXXX)
    const matchId = url.match(/\/api\/v1\/students\/([a-zA-Z0-9_-]+)$/);
    if (matchId && matchId[1] !== 'link-existing') {
      const studentId = matchId[1];
      const found = studentsStore.find(s => s.id === studentId);
      if (!found) {
        return throwError(() => createProblem(404, 'Not Found', `Student ${studentId} not found`, 'STUDENT_NOT_FOUND', url));
      }
      const profile: Student360Profile = {
        ...MOCK_STUDENT_360,
        ...found,
        id: found.id,
        studentCode: found.studentCode,
        fullName: found.fullName,
        grade: found.grade,
        guardianName: found.guardianName,
        guardianPhone: found.guardianPhone,
        whatsappOptIn: found.whatsappOptIn,
        activeNfcCardUid: found.activeNfcCardUid
      };
      return of(new HttpResponse({ status: 200, body: profile })).pipe(delay(60));
    }

    return of(new HttpResponse({
      status: 200,
      body: {
        items: filtered,
        totalCount: filtered.length,
        page: 1,
        pageSize: 25,
        totalPages: 1
      }
    })).pipe(delay(50));
  }

  // 2. POST /api/v1/students (Add new student wizard)
  if (url === '/api/v1/students' && method === 'POST') {
    const body = req.body;
    if (!body.fullName || body.fullName.trim().length < 3) {
      return throwError(() => createProblem(422, 'Validation Error', 'اسم الطالب يجب أن يكون 3 أحرف على الأقل', 'INVALID_STUDENT_NAME', url));
    }
    if (!body.guardianPhone || !ACADEMIC_VALIDATION_REGEX.EGYPTIAN_PHONE.test(body.guardianPhone)) {
      return throwError(() => createProblem(422, 'Validation Error', 'رقم ولي الأمر يجب أن يكون رقم مصري صحيح (11 رقم يبدأ بـ 010 أو 011 أو 012 أو 015)', 'INVALID_EGYPTIAN_PHONE', url));
    }

    const nextId = `std-${Date.now().toString().slice(-4)}`;
    const studentCode = body.studentCode || `ST-${(1100 + studentsStore.length)}`;
    const newStudent: StudentListItem = {
      id: nextId,
      workspaceId: 'ws-main',
      studentCode,
      fullName: body.fullName,
      grade: body.grade || 'Secondary1',
      gradeLabelAr: 'الأول الثانوي',
      gradeLabelEn: '1st Secondary',
      guardianName: body.guardianName || 'ولي أمر الطالب',
      guardianPhone: body.guardianPhone,
      whatsappOptIn: !!body.whatsappOptIn,
      activeNfcCardUid: body.nfcCardUid || null,
      qrIdentifierToken: `TQR-WS01-${studentCode.replace('-', '')}-NEW`,
      enrolledGroupCount: body.groupId ? 1 : 0,
      status: 'Active',
      withdrawnAt: null,
      attendanceRate: 100,
      unpaidBalance: 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    };

    studentsStore.unshift(newStudent);
    if (body.nfcCardUid) {
      cardsStore.unshift({
        id: `crd-${Date.now()}`,
        workspaceId: 'ws-main',
        cardUid: body.nfcCardUid,
        label: `كارت ${body.fullName}`,
        assignedStudentId: nextId,
        assignedStudentName: body.fullName,
        assignedStudentCode: studentCode,
        issuedAt: new Date().toISOString(),
        status: 'Active'
      });
    }

    return of(new HttpResponse({ status: 201, body: newStudent })).pipe(delay(100));
  }

  // 3. POST /api/v1/students/link-existing (DEC-02)
  if (url === '/api/v1/students/link-existing' && method === 'POST') {
    const { studentCode, pin } = req.body || {};
    if (!studentCode || !ACADEMIC_VALIDATION_REGEX.STUDENT_CODE.test(studentCode)) {
      return throwError(() => createProblem(422, 'Validation Error', 'صيغة كود الطالب غير صحيحة', 'INVALID_STUDENT_CODE', url));
    }
    if (!pin || !ACADEMIC_VALIDATION_REGEX.SECRET_PIN.test(pin)) {
      return throwError(() => createProblem(422, 'Validation Error', 'رمز PIN يجب أن يتكون من 4 أرقام', 'INVALID_PIN', url));
    }
    const found = studentsStore.find(s => s.studentCode === studentCode);
    if (!found) {
      return throwError(() => createProblem(404, 'Not Found', 'كود الطالب أو الرمز السري غير صحيح', 'STUDENT_NOT_FOUND', url));
    }
    return of(new HttpResponse({ status: 200, body: { success: true, student: found } })).pipe(delay(80));
  }

  // 4. GET /api/v1/attendance
  if (url.startsWith('/api/v1/attendance') && method === 'GET') {
    const urlObj = new URL(url, 'http://localhost');
    const sessionId = urlObj.searchParams.get('sessionId');
    const status = urlObj.searchParams.get('status');

    let filtered = [...attendanceStore];
    if (sessionId) {
      filtered = filtered.filter(a => a.sessionId === sessionId);
    }
    if (status && status !== 'All') {
      filtered = filtered.filter(a => a.status === status);
    }

    return of(new HttpResponse({
      status: 200,
      body: {
        items: filtered,
        totalCount: filtered.length,
        summary: {
          present: filtered.filter(a => a.status === 'Present').length,
          late: filtered.filter(a => a.status === 'Late').length,
          absent: filtered.filter(a => a.status === 'Absent').length,
          excused: filtered.filter(a => a.status === 'Excused').length,
          waiting: filtered.filter(a => a.status === 'Waiting').length,
          voided: filtered.filter(a => a.status === 'Voided').length
        }
      }
    })).pipe(delay(50));
  }

  // 5. POST /api/v1/attendance/scan (Live Station Credential Scanning SCR-05)
  if (url === '/api/v1/attendance/scan' && method === 'POST') {
    const { sessionId, identifier, method: scanMethod } = req.body || {};
    const session = sessionsStore.find(s => s.id === sessionId);
    if (!session) {
      return throwError(() => createProblem(404, 'Session Not Found', 'الحصة المحددة غير موجودة', 'SESSION_NOT_FOUND', url));
    }
    if (session.isCancelled || session.status === 'Cancelled') {
      return throwError(() => createProblem(409, 'Conflict', 'الحصة ملغاة ولا تقبل تسجيل حضور', 'SESSION_CANCELLED', url));
    }

    const cleanUid = (identifier || '').trim().toUpperCase();

    // Check Revoked Cards
    const revokedCard = cardsStore.find(c => c.cardUid.toUpperCase() === cleanUid && c.status === 'Revoked');
    if (revokedCard) {
      const result: CredentialScanResult = {
        outcome: 'Revoked',
        scannedAtUtc: new Date().toISOString(),
        soundToPlay: 'ErrorBuzz',
        messageAr: 'كارت ملغى أو تم استبداله',
        messageEn: 'Card Revoked or Replaced'
      };
      return of(new HttpResponse({ status: 200, body: result })).pipe(delay(50));
    }

    // Find student by card UID, student code, or phone
    const student = studentsStore.find(s =>
      (s.activeNfcCardUid && s.activeNfcCardUid.toUpperCase() === cleanUid) ||
      s.studentCode.toUpperCase() === cleanUid ||
      s.qrIdentifierToken === cleanUid ||
      s.guardianPhone === cleanUid
    );

    if (!student) {
      const result: CredentialScanResult = {
        outcome: 'Unregistered',
        scannedAtUtc: new Date().toISOString(),
        soundToPlay: 'ErrorBuzz',
        messageAr: 'كارت غير مسجل بالنظام',
        messageEn: 'Card Not Registered'
      };
      return of(new HttpResponse({ status: 200, body: result })).pipe(delay(50));
    }

    // Check duplicate
    const existing = attendanceStore.find(a => a.sessionId === sessionId && a.studentId === student.id && a.status !== 'Voided');
    if (existing && (existing.status === 'Present' || existing.status === 'Late')) {
      const result: CredentialScanResult = {
        outcome: 'Duplicate',
        recordId: existing.id,
        student: {
          id: student.id,
          studentCode: student.studentCode,
          fullName: student.fullName,
          guardianPhone: student.guardianPhone,
          whatsappOptIn: student.whatsappOptIn
        },
        scannedAtUtc: new Date().toISOString(),
        soundToPlay: 'DuplicateTone',
        messageAr: 'تم تسجيل الحضور مسبقاً',
        messageEn: 'Already Checked In'
      };
      return of(new HttpResponse({ status: 200, body: result })).pipe(delay(50));
    }

    // Cross-group check (DEC-06)
    if (student.grade !== 'Secondary1' && session.groupId === 'grp-01') {
      const result: CredentialScanResult = {
        outcome: 'WrongGroup',
        student: {
          id: student.id,
          studentCode: student.studentCode,
          fullName: student.fullName,
          guardianPhone: student.guardianPhone,
          whatsappOptIn: student.whatsappOptIn
        },
        originalGroupId: 'grp-03',
        originalGroupName: 'مجموعة ج - ثانية ثانوي',
        scannedAtUtc: new Date().toISOString(),
        soundToPlay: 'ErrorBuzz',
        messageAr: `طالب مسجل بمجموعة أخرى: مجموعة ج - ثانية ثانوي`,
        messageEn: 'Student registered in another group'
      };
      return of(new HttpResponse({ status: 200, body: result })).pipe(delay(50));
    }

    // Calculate Present vs Late
    const sessionStart = new Date(session.startsAt).getTime();
    const now = Date.now();
    const graceMs = (session.lateGraceMinutes || 15) * 60 * 1000;
    const isLate = now > (sessionStart + graceMs);
    const lateMinutes = isLate ? Math.max(1, Math.round((now - (sessionStart + graceMs)) / 60000)) : 0;

    const recordId = `att-${Date.now().toString().slice(-4)}`;
    const outcome = isLate ? 'Late' : 'Present';
    const newRecord: AttendanceRecordItem = {
      id: recordId,
      workspaceId: 'ws-main',
      sessionId,
      studentId: student.id,
      studentCode: student.studentCode,
      studentName: student.fullName,
      status: outcome,
      source: scanMethod === 'CameraQr' ? 'Qr' : scanMethod === 'WebNfc' ? 'WebNfc' : 'Nfc',
      checkInAt: new Date().toISOString(),
      autoEvaluationLocked: false,
      isLocked: false,
      whatsappNoticeStatus: student.whatsappOptIn ? 'Delivered' : 'NotSent'
    };

    attendanceStore.unshift(newRecord);

    const result: CredentialScanResult = {
      outcome,
      recordId,
      student: {
        id: student.id,
        studentCode: student.studentCode,
        fullName: student.fullName,
        guardianPhone: student.guardianPhone,
        whatsappOptIn: student.whatsappOptIn
      },
      scannedAtUtc: newRecord.checkInAt!,
      lateMinutes,
      soundToPlay: isLate ? 'LateChime' : 'PresentChime',
      messageAr: isLate ? `متأخر (${lateMinutes} دقيقة)` : 'حاضر في الموعد',
      messageEn: isLate ? `Late (${lateMinutes} min)` : 'Present On Time'
    };

    return of(new HttpResponse({ status: 200, body: result })).pipe(delay(60));
  }

  // 6. POST /api/v1/attendance/manual-override (DEC-07)
  if ((url === '/api/v1/attendance/manual-override' || url === '/api/v1/attendance/exceptional-substitute') && method === 'POST') {
    const { recordId, newStatus, reason, substituteReason, studentId, sessionId } = req.body || {};
    const effectiveReason = reason || substituteReason || '';
    const student = studentsStore.find(s => s.id === studentId);
    if (student && student.status === 'Withdrawn') {
      if (!effectiveReason || effectiveReason.trim().length < 10) {
        return throwError(() => createProblem(422, 'Validation Error', 'سبب التعديل للطالب المنسحب يجب ألا يقل عن 10 أحرف غير فارغة', 'INVALID_REASON_LENGTH', url));
      }
      const session = sessionsStore.find(s => s.id === sessionId);
      if (session && student.withdrawnAt && new Date(session.startsAt) > new Date(student.withdrawnAt)) {
        return throwError(() => createProblem(422, 'Historical Boundary Error', 'لا يمكن تسجيل حضور لطالب بعد تاريخ انسحابه الرسمي', 'ATTENDANCE_AFTER_WITHDRAWAL', url));
      }
    } else {
      if (!effectiveReason || effectiveReason.trim().length < 5) {
        return throwError(() => createProblem(422, 'Validation Error', 'سبب التعديل يجب ألا يقل عن 5 أحرف', 'INVALID_REASON_LENGTH', url));
      }
    }

    let record = attendanceStore.find(a => a.id === recordId);
    if (!record) {
      record = {
        id: recordId || `att-${Date.now().toString().slice(-4)}`,
        workspaceId: 'ws-main',
        sessionId,
        studentId,
        studentCode: student ? student.studentCode : 'ST-1000',
        studentName: student ? student.fullName : 'طالب',
        status: newStatus,
        source: 'Manual',
        checkInAt: new Date().toISOString(),
        autoEvaluationLocked: true,
        isLocked: true,
        overrideReason: effectiveReason,
        lockedAtUtc: new Date().toISOString()
      };
      attendanceStore.unshift(record);
    } else {
      record.status = newStatus;
      record.overrideReason = effectiveReason;
      record.autoEvaluationLocked = true;
      record.isLocked = true;
      record.lockedAtUtc = new Date().toISOString();
    }

    return of(new HttpResponse({ status: 200, body: record })).pipe(delay(60));
  }

  // 7. GET /api/v1/groups & POST /api/v1/groups
  if (url.startsWith('/api/v1/groups') && method === 'GET') {
    return of(new HttpResponse({ status: 200, body: groupsStore })).pipe(delay(50));
  }
  if (url === '/api/v1/groups' && method === 'POST') {
    const body = req.body || {};
    const newGroup: StudyGroupItem = {
      id: `grp-${Date.now().toString().slice(-4)}`,
      workspaceId: 'ws-main',
      name: body.name,
      grade: body.grade || 'Secondary1',
      gradeLabelAr: 'الأول الثانوي',
      gradeLabelEn: '1st Secondary',
      subject: body.subject || 'عام',
      teacherUserId: 'usr-teacher-01',
      teacherName: 'أ. محمد عبد الله',
      delegatedAssistantUserId: null,
      delegatedAssistantName: null,
      defaultMonthlyFee: Number(body.defaultMonthlyFee) || 250,
      lateGraceMinutes: Number(body.lateGraceMinutes) || 15,
      enrolledCount: 0,
      maxCapacity: Number(body.maxCapacity) || 30,
      scheduleDescription: body.scheduleDescription || 'مجدولة'
    };
    groupsStore.unshift(newGroup);
    return of(new HttpResponse({ status: 201, body: newGroup })).pipe(delay(80));
  }

  // 8. POST /api/v1/groups/:id/delegate-assistant (CONF-09 single delegate)
  if (url.match(/\/api\/v1\/groups\/[a-zA-Z0-9_-]+\/delegate-assistant$/) && method === 'POST') {
    const match = url.match(/\/api\/v1\/groups\/([a-zA-Z0-9_-]+)\/delegate-assistant$/);
    const groupId = match![1];
    const { assistantUserId, assistantName } = req.body || {};
    const grp = groupsStore.find(g => g.id === groupId);
    if (grp) {
      grp.delegatedAssistantUserId = assistantUserId || null;
      grp.delegatedAssistantName = assistantName || null;
    }
    return of(new HttpResponse({ status: 200, body: grp })).pipe(delay(60));
  }

  // 9. GET /api/v1/schedule/sessions
  if ((url.startsWith('/api/v1/schedule/sessions') || url.startsWith('/api/v1/sessions')) && method === 'GET') {
    return of(new HttpResponse({ status: 200, body: sessionsStore })).pipe(delay(50));
  }

  // 10. POST /api/v1/schedule/sessions (Schedule session)
  if ((url === '/api/v1/schedule/sessions' || url === '/api/v1/sessions') && method === 'POST') {
    const body = req.body || {};
    const newSession: ClassSessionItem = {
      id: `ses-${Date.now().toString().slice(-4)}`,
      workspaceId: 'ws-main',
      groupId: body.groupId,
      groupName: body.groupName || 'مجموعة دراسية',
      subject: body.subject || 'الرياضيات',
      startsAt: body.startsAt,
      endsAt: body.endsAt,
      lateGraceMinutes: Number(body.lateGraceMinutes) || 15,
      status: 'Scheduled',
      roomName: body.roomName || 'قاعة 1',
      presentCount: 0,
      lateCount: 0,
      absentCount: 0,
      excusedCount: 0,
      waitingCount: 20,
      voidedCount: 0,
      isCancelled: false,
      cancellationReason: null
    };
    sessionsStore.push(newSession);
    return of(new HttpResponse({ status: 201, body: newSession })).pipe(delay(70));
  }

  // 11. POST /api/v1/schedule/sessions/:id/reschedule (DEC-03)
  if (url.match(/\/api\/v1\/schedule\/sessions\/[a-zA-Z0-9_-]+\/reschedule$/) && method === 'POST') {
    const match = url.match(/\/api\/v1\/schedule\/sessions\/([a-zA-Z0-9_-]+)\/reschedule$/);
    const sessionId = match![1];
    const { startsAt, endsAt } = req.body || {};
    const session = sessionsStore.find(s => s.id === sessionId);
    if (session) {
      session.startsAt = startsAt || session.startsAt;
      session.endsAt = endsAt || session.endsAt;
      // Re-evaluate unlocked rows
      attendanceStore.forEach(a => {
        if (a.sessionId === sessionId && !a.autoEvaluationLocked) {
          // Keep intact or recalculate status
        }
      });
    }
    return of(new HttpResponse({ status: 200, body: session })).pipe(delay(60));
  }

  // 12. POST /api/v1/schedule/sessions/:id/cancel (DEC-01)
  if (url.match(/\/api\/v1\/schedule\/sessions\/[a-zA-Z0-9_-]+\/cancel$/) && method === 'POST') {
    const match = url.match(/\/api\/v1\/schedule\/sessions\/([a-zA-Z0-9_-]+)\/cancel$/);
    const sessionId = match![1];
    const { reason } = req.body || {};
    if (!reason || reason.trim().length < 5) {
      return throwError(() => createProblem(422, 'Validation Error', 'سبب الإلغاء يجب ألا يقل عن 5 أحرف', 'INVALID_CANCEL_REASON', url));
    }
    const session = sessionsStore.find(s => s.id === sessionId);
    if (session) {
      session.status = 'Cancelled';
      session.isCancelled = true;
      session.cancellationReason = reason;
      // Void all attendance rows
      attendanceStore.forEach(a => {
        if (a.sessionId === sessionId) {
          a.status = 'Voided';
          a.voidedAtUtc = new Date().toISOString();
          a.voidReason = 'Session Cancelled';
        }
      });
    }
    return of(new HttpResponse({ status: 200, body: session })).pipe(delay(70));
  }

  // 13. GET /api/v1/cards & POST /api/v1/cards/link & POST /api/v1/cards/revoke
  if (url.startsWith('/api/v1/cards') && method === 'GET') {
    return of(new HttpResponse({ status: 200, body: cardsStore })).pipe(delay(50));
  }
  if (url === '/api/v1/cards/link' && method === 'POST') {
    const { cardUid, studentId } = req.body || {};
    const student = studentsStore.find(s => s.id === studentId);
    let card = cardsStore.find(c => c.cardUid.toUpperCase() === (cardUid || '').toUpperCase());
    if (!card) {
      card = {
        id: `crd-${Date.now()}`,
        workspaceId: 'ws-main',
        cardUid,
        label: `كارت ${student ? student.fullName : 'جديد'}`,
        assignedStudentId: studentId,
        assignedStudentName: student ? student.fullName : null,
        assignedStudentCode: student ? student.studentCode : null,
        issuedAt: new Date().toISOString(),
        status: 'Active'
      };
      cardsStore.unshift(card);
    } else {
      card.assignedStudentId = studentId;
      card.assignedStudentName = student ? student.fullName : null;
      card.assignedStudentCode = student ? student.studentCode : null;
      card.status = 'Active';
    }
    if (student) {
      student.activeNfcCardUid = cardUid;
    }
    return of(new HttpResponse({ status: 200, body: card })).pipe(delay(70));
  }
  if (url === '/api/v1/cards/revoke' && method === 'POST') {
    const { cardUid, reason } = req.body || {};
    if (!reason || reason.trim().length < 5) {
      return throwError(() => createProblem(422, 'Validation Error', 'سبب إلغاء الكارت يجب ألا يقل عن 5 أحرف', 'INVALID_REVOCATION_REASON', url));
    }
    const card = cardsStore.find(c => c.cardUid.toUpperCase() === (cardUid || '').toUpperCase());
    if (card) {
      card.status = 'Revoked';
      card.revocationReason = reason;
      const student = studentsStore.find(s => s.id === card.assignedStudentId);
      if (student && student.activeNfcCardUid === card.cardUid) {
        student.activeNfcCardUid = null;
      }
    }
    return of(new HttpResponse({ status: 200, body: card })).pipe(delay(70));
  }

  // 14. POST /api/v1/sync/batch & POST /api/v1/attendance/resolve-conflict (DEC-04)
  if (url === '/api/v1/sync/batch' && method === 'POST') {
    const items: any[] = req.body?.items || [];
    const syncedIds: number[] = [];
    const conflicts: any[] = [];

    items.forEach(it => {
      const session = sessionsStore.find(s => s.id === it.sessionId);
      if (session && (session.isCancelled || session.status === 'Cancelled')) {
        conflicts.push({
          localId: it.localId,
          operationId: it.operationId,
          studentId: it.studentId,
          sessionId: it.sessionId,
          conflictReason: 'SessionCancelledOnline',
          cancellationReason: session.cancellationReason || 'الحصة ألغيت أونلاين'
        });
      } else {
        syncedIds.push(it.localId);
      }
    });

    return of(new HttpResponse({
      status: 200,
      body: {
        syncedLocalIds: syncedIds,
        conflicts
      }
    })).pipe(delay(90));
  }

  if (url === '/api/v1/attendance/resolve-conflict' && method === 'POST') {
    const { operationId, decision, reason } = req.body || {};
    return of(new HttpResponse({
      status: 200,
      body: {
        operationId,
        resolved: true,
        decision,
        reason
      }
    })).pipe(delay(60));
  }

  return null;
}
