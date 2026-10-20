import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NfcCardItem, CardRevocationReason } from '../../../../../core/models/academic.models';

@Component({
  selector: 'app-revoke-card-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './revoke-card-modal.component.html',
  styleUrls: ['./revoke-card-modal.component.scss']
})
export class RevokeCardModalComponent {
  readonly card = input.required<NfcCardItem>();

  readonly revoked = output<{ cardUid: string; reason: string }>();
  readonly closed = output<void>();

  revocationReason: CardRevocationReason = 'Lost';
  customNotes = '';
  validationError = signal<string | null>(null);

  confirm(): void {
    if (!this.revocationReason) {
      this.validationError.set('يرجى اختيار سبب الإلغاء الإلزامي (CONF-03)');
      return;
    }

    const reasonText = `${this.revocationReason}${this.customNotes.trim() ? ': ' + this.customNotes.trim() : ''}`;
    this.revoked.emit({
      cardUid: this.card().cardUid,
      reason: reasonText
    });
  }

  close(): void {
    this.closed.emit();
  }
}
