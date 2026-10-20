import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  StudentListItem,
  Student360Profile,
  StudyGroupItem,
  ClassSessionItem,
  AttendanceRecordItem,
  NfcCardItem,
  SyncBatchRequest,
  SyncBatchResponse,
  AttendanceBadgeStatus
} from '../models/academic.models';

@Injectable({
  providedIn: 'root'
})
export class AcademicService {
  private readonly http = inject(HttpClient);

  // --- Students ---
  getStudents(params?: { search?: string; grade?: string; status?: string }): Observable<StudentListItem[]> {
    let httpParams = new HttpParams();
    if (params?.search) httpParams = httpParams.set('search', params.search);
    if (params?.grade) httpParams = httpParams.set('grade', params.grade);
    if (params?.status) httpParams = httpParams.set('status', params.status);
    return this.http.get<StudentListItem[]>('/api/v1/students', { params: httpParams });
  }

  getStudent360(id: string): Observable<Student360Profile> {
    return this.http.get<Student360Profile>(`/api/v1/students/${id}`);
  }

  createStudent(payload: {
    fullName: string;
    grade: string;
    studentCode?: string;
    guardianName: string;
    guardianPhone: string;
    whatsappOptIn: boolean;
    groupIds: string[];
    initialNfcCardUid?: string;
    defaultMonthlyFee?: number;
  }): Observable<StudentListItem> {
    return this.http.post<StudentListItem>('/api/v1/students', payload);
  }

  linkExistingStudent(payload: { studentCode: string; secretPin: string }): Observable<StudentListItem> {
    return this.http.post<StudentListItem>('/api/v1/students/link-existing', payload);
  }

  // --- Study Groups ---
  getStudyGroups(): Observable<StudyGroupItem[]> {
    return this.http.get<StudyGroupItem[]>('/api/v1/groups');
  }

  createStudyGroup(payload: Partial<StudyGroupItem>): Observable<StudyGroupItem> {
    return this.http.post<StudyGroupItem>('/api/v1/groups', payload);
  }

  delegateGroupAssistant(groupId: string, assistantStaffId: string | null): Observable<StudyGroupItem> {
    return this.http.patch<StudyGroupItem>(`/api/v1/groups/${groupId}/delegate-assistant`, {
      assistantStaffId
    });
  }

  // --- Class Sessions ---
  getClassSessions(groupId?: string): Observable<ClassSessionItem[]> {
    let params = new HttpParams();
    if (groupId) params = params.set('groupId', groupId);
    return this.http.get<ClassSessionItem[]>('/api/v1/sessions', { params });
  }

  createClassSession(payload: Partial<ClassSessionItem>): Observable<ClassSessionItem> {
    return this.http.post<ClassSessionItem>('/api/v1/sessions', payload);
  }

  rescheduleSession(sessionId: string, newStartTime: string, newEndTime: string): Observable<ClassSessionItem> {
    return this.http.post<ClassSessionItem>(`/api/v1/sessions/${sessionId}/reschedule`, {
      newStartTime,
      newEndTime
    });
  }

  cancelSession(sessionId: string, reason: string): Observable<ClassSessionItem> {
    return this.http.post<ClassSessionItem>(`/api/v1/sessions/${sessionId}/cancel`, {
      reason
    });
  }

  // --- Attendance Records ---
  getAttendanceRecords(params?: {
    sessionId?: string;
    date?: string;
    groupId?: string;
    status?: string;
  }): Observable<AttendanceRecordItem[]> {
    let httpParams = new HttpParams();
    if (params?.sessionId) httpParams = httpParams.set('sessionId', params.sessionId);
    if (params?.date) httpParams = httpParams.set('date', params.date);
    if (params?.groupId) httpParams = httpParams.set('groupId', params.groupId);
    if (params?.status) httpParams = httpParams.set('status', params.status);
    return this.http.get<AttendanceRecordItem[]>('/api/v1/attendance', { params: httpParams });
  }

  overrideAttendance(
    recordId: string,
    status: AttendanceBadgeStatus,
    justificationReason: string
  ): Observable<AttendanceRecordItem> {
    return this.http.patch<AttendanceRecordItem>(`/api/v1/attendance/${recordId}/override`, {
      status,
      justificationReason
    });
  }

  // --- NFC Cards ---
  getNfcCards(): Observable<NfcCardItem[]> {
    return this.http.get<NfcCardItem[]>('/api/v1/cards');
  }

  linkCard(cardUid: string, studentId: string): Observable<NfcCardItem> {
    return this.http.post<NfcCardItem>('/api/v1/cards/link', { cardUid, studentId });
  }

  revokeCard(cardUid: string, reason: string): Observable<NfcCardItem> {
    return this.http.post<NfcCardItem>(`/api/v1/cards/${encodeURIComponent(cardUid)}/revoke`, { reason });
  }

  // --- Offline Batch Sync ---
  syncBatch(payload: SyncBatchRequest): Observable<SyncBatchResponse> {
    return this.http.post<SyncBatchResponse>('/api/v1/sync/batch', payload);
  }
}
