import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StudyGroupItem, ClassSessionItem } from '../../../../../core/models/academic.models';

@Component({
  selector: 'app-schedule-session-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './schedule-session-modal.component.html',
  styleUrls: ['./schedule-session-modal.component.scss']
})
export class ScheduleSessionModalComponent {
  readonly groups = input<StudyGroupItem[]>([]);

  readonly scheduled = output<Partial<ClassSessionItem>>();
  readonly closed = output<void>();

  selectedGroupId = '';
  sessionDate = new Date().toISOString().split('T')[0];
  startTime = '14:00';
  endTime = '16:00';
  lateGraceMinutes = 15;
  roomName = 'قاعة الفارابي';

  validationError = signal<string | null>(null);

  get isValid(): boolean {
    return !!this.selectedGroupId && !!this.sessionDate && !!this.startTime && !!this.endTime && !!this.roomName.trim();
  }

  submit(): void {
    if (!this.isValid) {
      this.validationError.set('يرجى اختيار المجموعة وتحديد تاريخ ووقت القاعة بشكل صحيح');
      return;
    }

    const group = this.groups().find(g => g.id === this.selectedGroupId);
    const startsAt = `${this.sessionDate}T${this.startTime}:00Z`;
    const endsAt = `${this.sessionDate}T${this.endTime}:00Z`;

    this.validationError.set(null);
    this.scheduled.emit({
      groupId: this.selectedGroupId,
      groupName: group?.name || 'مجموعة دراسية',
      grade: group?.grade || 'Grade1Secondary',
      roomName: this.roomName.trim(),
      startsAt,
      endsAt,
      lateGraceMinutes: this.lateGraceMinutes,
      status: 'Scheduled',
      totalRosterCount: group?.currentEnrollmentCount || 0,
      presentCount: 0,
      lateCount: 0,
      absentCount: 0
    });
  }

  close(): void {
    this.closed.emit();
  }
}
