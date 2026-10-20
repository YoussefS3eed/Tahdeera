import { Component, input, output, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardRevocationReason } from '../../../../../core/models/academic.models';
import { NfcWedgeAdapterService } from '../../../../../core/services/nfc-wedge-adapter.service';

@Component({
  selector: 'app-card-management-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './card-management-modal.component.html',
  styleUrls: ['./card-management-modal.component.scss']
})
export class CardManagementModalComponent implements OnInit {
  private readonly nfcWedge = inject(NfcWedgeAdapterService);

  readonly currentCardUid = input<string | null>(null);
  readonly studentName = input.required<string>();
  readonly studentCode = input.required<string>();

  readonly cardRevoked = output<{ reason: CardRevocationReason }>();
  readonly cardReplaced = output<{ newUid: string }>();
  readonly closed = output<void>();

  activeMode = signal<'view' | 'revoke' | 'replace'>('view');
  revocationReason: CardRevocationReason = 'Lost';
  newCardUid = '';
  validationError = signal<string | null>(null);

  ngOnInit(): void {
    this.nfcWedge.uidScanned$.subscribe(uid => {
      if (this.activeMode() === 'replace') {
        this.newCardUid = uid;
      }
    });
  }

  setMode(mode: 'view' | 'revoke' | 'replace'): void {
    this.activeMode.set(mode);
    this.validationError.set(null);
  }

  confirmRevocation(): void {
    if (!this.revocationReason) {
      this.validationError.set('يرجى تحديد سبب إلغاء الكارت (CONF-03)');
      return;
    }
    this.cardRevoked.emit({ reason: this.revocationReason });
  }

  confirmReplacement(): void {
    const trimmed = this.newCardUid.trim();
    if (!trimmed || trimmed.length < 4) {
      this.validationError.set('يرجى تمرير كارت NFC الجديد أو إدخال معرف صالح');
      return;
    }
    this.cardReplaced.emit({ newUid: trimmed });
  }

  printCardStub(): void {
    window.print();
  }

  close(): void {
    this.closed.emit();
  }
}
