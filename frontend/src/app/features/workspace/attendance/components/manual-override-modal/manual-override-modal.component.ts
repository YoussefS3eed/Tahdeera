import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AttendanceBadgeStatus, AttendanceRecordItem } from '../../../../../core/models/academic.models';

@Component({
  selector: 'app-manual-override-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './manual-override-modal.component.html',
  styleUrls: ['./manual-override-modal.component.scss']
})
export class ManualOverrideModalComponent {
  readonly record = input.required<AttendanceRecordItem>();

  readonly saved = output<{ status: AttendanceBadgeStatus; reason: string }>();
  readonly closed = output<void>();

  selectedStatus: AttendanceBadgeStatus = 'Present';
  overrideReason = '';
  validationError: string | null = null;

  ngOnInit(): void {
    if (this.record()) {
      this.selectedStatus = this.record().status === 'Waiting' ? 'Present' : this.record().status;
    }
  }

  save(): void {
    const trimmed = this.overrideReason.trim();
    if (trimmed.length < 5) {
      this.validationError = 'سبب التعديل يجب ألا يقل عن 5 أحرف غير فارغة';
      return;
    }

    this.validationError = null;
    this.saved.emit({
      status: this.selectedStatus,
      reason: trimmed
    });
  }

  close(): void {
    this.closed.emit();
  }
}
