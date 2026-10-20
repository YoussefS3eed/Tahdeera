import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AcademicService } from '../../../core/services/academic.service';
import { AttendanceSyncService } from '../../../core/services/attendance-sync.service';
import {
  AttendanceRecordItem,
  StudyGroupItem,
  ClassSessionItem,
  AttendanceBadgeStatus,
  StudentListItem
} from '../../../core/models/academic.models';
import { ManualOverrideModalComponent } from './components/manual-override-modal/manual-override-modal.component';
import { WithdrawnRetroactiveModalComponent } from './components/withdrawn-retroactive-modal/withdrawn-retroactive-modal.component';
import { PostSyncReviewModalComponent, ConflictedSyncItem } from './components/post-sync-review-modal/post-sync-review-modal.component';

@Component({
  selector: 'app-attendance',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ManualOverrideModalComponent,
    WithdrawnRetroactiveModalComponent,
    PostSyncReviewModalComponent
  ],
  templateUrl: './attendance.component.html',
  styleUrls: ['./attendance.component.scss']
})
export class AttendanceComponent implements OnInit {
  private readonly academicService = inject(AcademicService);
  readonly syncService = inject(AttendanceSyncService);

  // Filters & State
  selectedDate = signal(new Date().toISOString().split('T')[0]);
  selectedGroupId = signal<string>('all');
  selectedSessionId = signal<string>('all');
  activeTab = signal<string>('All');
  pageSize = signal(10);
  currentPage = signal(1);

  // Data Stores
  groups = signal<StudyGroupItem[]>([]);
  sessions = signal<ClassSessionItem[]>([]);
  allRecords = signal<AttendanceRecordItem[]>([]);
  studentsMap = signal<Map<string, StudentListItem>>(new Map());
  isLoading = signal(false);

  // Modals state
  activeOverrideRecord = signal<AttendanceRecordItem | null>(null);
  activeWithdrawnRecord = signal<AttendanceRecordItem | null>(null);
  showConflictModal = signal(false);
  conflictedItems = signal<ConflictedSyncItem[]>([]);

  ngOnInit(): void {
    this.loadInitialData();
  }

  loadInitialData(): void {
    this.isLoading.set(true);

    this.academicService.getStudyGroups().subscribe(groups => {
      this.groups.set(groups);
    });

    this.academicService.getClassSessions().subscribe(sessions => {
      this.sessions.set(sessions);
    });

    this.academicService.getStudents().subscribe(students => {
      const map = new Map<string, StudentListItem>();
      students.forEach(s => map.set(s.id, s));
      this.studentsMap.set(map);
    });

    this.loadRecords();
  }

  loadRecords(): void {
    this.isLoading.set(true);
    const params: { sessionId?: string; date?: string; groupId?: string } = {};
    if (this.selectedGroupId() !== 'all') params.groupId = this.selectedGroupId();
    if (this.selectedSessionId() !== 'all') params.sessionId = this.selectedSessionId();
    if (this.selectedDate()) params.date = this.selectedDate();

    this.academicService.getAttendanceRecords(params).subscribe({
      next: records => {
        this.allRecords.set(records);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  // Filtered by Group / Session
  filteredSessions = computed(() => {
    const gid = this.selectedGroupId();
    if (gid === 'all') return this.sessions();
    return this.sessions().filter(s => s.groupId === gid);
  });

  // Filtered Records based on Active Tab
  filteredRecords = computed(() => {
    const tab = this.activeTab();
    const records = this.allRecords();
    if (tab === 'All') return records;
    return records.filter(r => r.status === tab);
  });

  // Paginated records
  paginatedRecords = computed(() => {
    const list = this.filteredRecords();
    const page = this.currentPage();
    const size = this.pageSize();
    const start = (page - 1) * size;
    return list.slice(start, start + size);
  });

  totalPages = computed(() => {
    return Math.ceil(this.filteredRecords().length / this.pageSize()) || 1;
  });

  // Tab count helpers
  getCount(status: string): number {
    if (status === 'All') return this.allRecords().length;
    return this.allRecords().filter(r => r.status === status).length;
  }

  onFilterChange(): void {
    this.currentPage.set(1);
    this.loadRecords();
  }

  setTab(tab: string): void {
    this.activeTab.set(tab);
    this.currentPage.set(1);
  }

  setPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  // Modal Actions
  openEditModal(record: AttendanceRecordItem): void {
    const student = this.studentsMap().get(record.studentId);
    if (student && student.status === 'Withdrawn') {
      // DEC-07: Dedicated Withdrawn Modal
      this.activeWithdrawnRecord.set(record);
    } else {
      this.activeOverrideRecord.set(record);
    }
  }

  closeModals(): void {
    this.activeOverrideRecord.set(null);
    this.activeWithdrawnRecord.set(null);
  }

  saveOverride(event: { status: AttendanceBadgeStatus; reason: string }): void {
    const record = this.activeOverrideRecord();
    if (!record) return;

    this.academicService.overrideAttendance(record.id, event.status, event.reason).subscribe(updated => {
      this.allRecords.update(prev => prev.map(r => r.id === updated.id ? updated : r));
      this.closeModals();
    });
  }

  saveWithdrawnOverride(event: { status: AttendanceBadgeStatus; reason: string }): void {
    const record = this.activeWithdrawnRecord();
    if (!record) return;

    this.academicService.overrideAttendance(record.id, event.status, event.reason).subscribe(updated => {
      this.allRecords.update(prev => prev.map(r => r.id === updated.id ? updated : r));
      this.closeModals();
    });
  }

  getStudentWithdrawnDate(studentId: string): string | null {
    const s = this.studentsMap().get(studentId);
    return s?.withdrawnAt || null;
  }

  // Conflicts Review Modal
  openConflicts(): void {
    this.conflictedItems.set([
      {
        operationId: 'op-conf-1',
        workspaceId: 'ws-cairo-01',
        sessionId: 'sess-03',
        studentId: 'std-1044',
        identifier: '04:A2:7C:19',
        method: 'NfcCard',
        scannedAtUtc: '2026-10-21T09:15:00Z',
        syncStatus: 'Conflict',
        conflictReason: 'SessionCancelledOnline: ظروف طارئة ألغت الجلسة',
        studentName: 'محمود طارق عبد العزيز',
        studentCode: 'ST-1044'
      }
    ]);
    this.showConflictModal.set(true);
  }

  resolveConflictItem(event: { operationId: string; decision: 'ApproveExceptional' | 'Void'; justificationReason?: string }): void {
    this.syncService.resolveConflict(event.operationId, event.decision, event.justificationReason);
    this.conflictedItems.update(prev => prev.filter(i => i.operationId !== event.operationId));
    if (this.conflictedItems().length === 0) {
      this.showConflictModal.set(false);
    }
  }

  exportToExcel(): void {
    const rows = this.filteredRecords().map(r => ({
      Code: r.studentCode,
      Name: r.studentName,
      Status: r.status,
      Time: r.scannedAt || '-',
      Locked: r.autoEvaluationLocked ? 'نعم' : 'لا'
    }));
    const blob = new Blob([JSON.stringify(rows, null, 2)], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `attendance-log-${this.selectedDate()}.json`;
    a.click();
    window.URL.revokeObjectURL(url);
  }
}
