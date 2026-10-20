import { Component, input, output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AttendanceBadgeStatus, AttendanceRecordItem } from '../../../../../core/models/academic.models';

@Component({
  selector: 'app-withdrawn-retroactive-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './withdrawn-retroactive-modal.component.html',
  styleUrls: ['./withdrawn-retroactive-modal.component.scss']
})
export class WithdrawnRetroactiveModalComponent implements OnInit {
  readonly record = input.required<AttendanceRecordItem>();
  readonly withdrawnAt = input<string | null>(null);
  readonly sessionDate = input<string>('');

  readonly saved = output<{ status: AttendanceBadgeStatus; reason: string }>();
  readonly closed = output<void>();

  selectedStatus: AttendanceBadgeStatus = 'Present';
  overrideReason = '';
  validationError: string | null = null;
  isBlockedByDate = false;

  ngOnInit(): void {
    if (this.record()) {
      this.selectedStatus = this.record().status === 'Waiting' ? 'Present' : this.record().status;
    }

    // Historical Boundary Check per DEC-07
    this.checkHistoricalBoundary();
  }

  checkHistoricalBoundary(): void {
    const wAt = this.withdrawnAt();
    const sDate = this.sessionDate() || this.record()?.scannedAt || '';
    if (wAt && sDate) {
      const sessionTime = new Date(sDate).getTime();
      const withdrawnTime = new Date(wAt).getTime();
      if (!isNaN(sessionTime) && !isNaN(withdrawnTime) && sessionTime > withdrawnTime) {
        this.isBlockedByDate = true;
        this.validationError = 'لا يمكن تسجيل أو تعديل حضور لجلسة بعد تاريخ انسحاب الطالب';
      }
    }
  }

  get isReasonValid(): boolean {
    return this.overrideReason.trim().length >= 10;
  }

  save(): void {
    if (this.isBlockedByDate) {
      return;
    }

    const trimmed = this.overrideReason.trim();
    if (trimmed.length < 10) {
      this.validationError = 'سبب التعديل يجب ألا يقل عن 10 أحرف غير فارغة';
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
