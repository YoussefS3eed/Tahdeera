import { Component, input, output, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ClassSessionItem } from '../../../../../core/models/academic.models';

@Component({
  selector: 'app-reschedule-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reschedule-modal.component.html',
  styleUrls: ['./reschedule-modal.component.scss']
})
export class RescheduleModalComponent implements OnInit {
  readonly session = input.required<ClassSessionItem>();

  readonly rescheduled = output<{ sessionId: string; newStartTime: string; newEndTime: string }>();
  readonly closed = output<void>();

  sessionDate = '';
  newStartTime = '';
  newEndTime = '';
  validationError = signal<string | null>(null);

  ngOnInit(): void {
    if (this.session()) {
      const s = this.session();
      this.sessionDate = s.startsAt.split('T')[0];
      this.newStartTime = s.startsAt.split('T')[1].substring(0, 5);
      this.newEndTime = s.endsAt.split('T')[1].substring(0, 5);
    }
  }

  get isValid(): boolean {
    return !!this.sessionDate && !!this.newStartTime && !!this.newEndTime;
  }

  submit(): void {
    if (!this.isValid) {
      this.validationError.set('يرجى تحديد وقت البدء ووقت الانتهاء بشكل صالح');
      return;
    }

    const startIso = `${this.sessionDate}T${this.newStartTime}:00Z`;
    const endIso = `${this.sessionDate}T${this.newEndTime}:00Z`;

    this.validationError.set(null);
    this.rescheduled.emit({
      sessionId: this.session().id,
      newStartTime: startIso,
      newEndTime: endIso
    });
  }

  close(): void {
    this.closed.emit();
  }
}
