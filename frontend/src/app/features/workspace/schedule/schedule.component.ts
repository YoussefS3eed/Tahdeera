import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AcademicService } from '../../../core/services/academic.service';
import { ClassSessionItem, StudyGroupItem } from '../../../core/models/academic.models';
import { ScheduleSessionModalComponent } from './components/schedule-session-modal/schedule-session-modal.component';
import { RescheduleModalComponent } from './components/reschedule-modal/reschedule-modal.component';
import { CancelSessionModalComponent } from './components/cancel-session-modal/cancel-session-modal.component';

@Component({
  selector: 'app-schedule',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ScheduleSessionModalComponent,
    RescheduleModalComponent,
    CancelSessionModalComponent
  ],
  templateUrl: './schedule.component.html',
  styleUrls: ['./schedule.component.scss']
})
export class ScheduleComponent implements OnInit {
  private readonly academicService = inject(AcademicService);

  sessions = signal<ClassSessionItem[]>([]);
  groups = signal<StudyGroupItem[]>([]);
  isLoading = signal(false);

  // Filters
  selectedGroupId = signal('all');

  // Modals
  showScheduleModal = signal(false);
  activeRescheduleSession = signal<ClassSessionItem | null>(null);
  activeCancelSession = signal<ClassSessionItem | null>(null);

  ngOnInit(): void {
    this.loadGroups();
    this.loadSessions();
  }

  loadGroups(): void {
    this.academicService.getStudyGroups().subscribe(groups => {
      this.groups.set(groups);
    });
  }

  loadSessions(): void {
    this.isLoading.set(true);
    this.academicService.getClassSessions().subscribe({
      next: sessions => {
        this.sessions.set(sessions);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  filteredSessions = computed(() => {
    const gid = this.selectedGroupId();
    if (gid === 'all') return this.sessions();
    return this.sessions().filter(s => s.groupId === gid);
  });

  onSessionScheduled(payload: Partial<ClassSessionItem>): void {
    this.academicService.createClassSession(payload).subscribe(newSess => {
      this.sessions.update(prev => [newSess, ...prev]);
      this.showScheduleModal.set(false);
    });
  }

  openReschedule(sess: ClassSessionItem): void {
    this.activeRescheduleSession.set(sess);
  }

  onSessionRescheduled(event: { sessionId: string; newStartTime: string; newEndTime: string }): void {
    this.academicService.rescheduleSession(event.sessionId, event.newStartTime, event.newEndTime).subscribe(updated => {
      this.sessions.update(prev => prev.map(s => s.id === updated.id ? updated : s));
      this.activeRescheduleSession.set(null);
    });
  }

  openCancel(sess: ClassSessionItem): void {
    this.activeCancelSession.set(sess);
  }

  onSessionCancelled(event: { sessionId: string; reason: string }): void {
    this.academicService.cancelSession(event.sessionId, event.reason).subscribe(cancelled => {
      this.sessions.update(prev => prev.map(s => s.id === cancelled.id ? cancelled : s));
      this.activeCancelSession.set(null);
    });
  }
}
