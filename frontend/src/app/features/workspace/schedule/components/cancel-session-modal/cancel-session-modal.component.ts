import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ClassSessionItem } from '../../../../../core/models/academic.models';

@Component({
  selector: 'app-cancel-session-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cancel-session-modal.component.html',
  styleUrls: ['./cancel-session-modal.component.scss']
})
export class CancelSessionModalComponent {
  readonly session = input.required<ClassSessionItem>();

  readonly cancelled = output<{ sessionId: string; reason: string }>();
  readonly closed = output<void>();

  cancellationReason = '';
  validationError = signal<string | null>(null);

  get isReasonValid(): boolean {
    return this.cancellationReason.trim().length >= 5;
  }

  submit(): void {
    const trimmed = this.cancellationReason.trim();
    if (trimmed.length < 5) {
      this.validationError.set('سبب الإلغاء يجب ألا يقل عن 5 أحرف غير فارغة (DEC-01)');
      return;
    }

    this.validationError.set(null);
    this.cancelled.emit({
      sessionId: this.session().id,
      reason: trimmed
    });
  }

  close(): void {
    this.closed.emit();
  }
}
