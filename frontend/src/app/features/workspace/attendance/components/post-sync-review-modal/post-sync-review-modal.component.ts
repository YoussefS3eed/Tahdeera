import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalAttendanceRow } from '../../../../../core/models/academic.models';

export interface ConflictedSyncItem {
  operationId: string;
  workspaceId?: string;
  sessionId?: string;
  studentId?: string;
  studentName?: string;
  studentCode?: string;
  sessionTitle?: string;
  scannedAtUtc: string;
  conflictReason?: string;
  identifier?: string;
  method?: string;
  outcome?: string;
  syncStatus?: string;
}

@Component({
  selector: 'app-post-sync-review-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './post-sync-review-modal.component.html',
  styleUrls: ['./post-sync-review-modal.component.scss']
})
export class PostSyncReviewModalComponent {
  readonly conflicts = input<ConflictedSyncItem[]>([]);

  readonly resolved = output<{
    operationId: string;
    decision: 'ApproveExceptional' | 'Void';
    justificationReason?: string;
  }>();

  readonly closed = output<void>();

  voidReason: { [opId: string]: string } = {};
  showVoidInput: { [opId: string]: boolean } = {};
  voidError: { [opId: string]: string | null } = {};

  toggleVoid(opId: string): void {
    this.showVoidInput[opId] = !this.showVoidInput[opId];
    this.voidError[opId] = null;
  }

  approveExceptional(item: ConflictedSyncItem): void {
    this.resolved.emit({
      operationId: item.operationId,
      decision: 'ApproveExceptional'
    });
  }

  confirmVoid(item: ConflictedSyncItem): void {
    const reason = (this.voidReason[item.operationId] || '').trim();
    if (reason.length < 5) {
      this.voidError[item.operationId] = 'سبب الإلغاء يجب ألا يقل عن 5 أحرف';
      return;
    }

    this.voidError[item.operationId] = null;
    this.resolved.emit({
      operationId: item.operationId,
      decision: 'Void',
      justificationReason: reason
    });
  }

  close(): void {
    this.closed.emit();
  }
}
