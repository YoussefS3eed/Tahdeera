import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FinancialTransaction } from '../../../../../core/models/finance.models';
import { FinanceStore } from '../../../../../core/stores/finance.store';

@Component({
  selector: 'app-payment-reversal-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payment-reversal-modal.component.html',
  styleUrls: ['./payment-reversal-modal.component.scss']
})
export class PaymentReversalModalComponent {
  private financeStore = inject(FinanceStore);

  @Input() transaction: FinancialTransaction | null = null;
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() reversalSuccess = new EventEmitter<any>();

  reason = signal<string>('');
  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  ngOnChanges(): void {
    this.reason.set('');
    this.errorMessage.set(null);
  }

  async confirmReversal(): Promise<void> {
    if (!this.transaction) return;
    const r = this.reason().trim();
    if (r.length < 5) {
      this.errorMessage.set('يرجى توضيح سبب إلغاء المعاملة (5 أحرف على الأقل)');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    try {
      const res = await this.financeStore.reversePayment(this.transaction.id, r);
      this.reversalSuccess.emit(res);
      this.closeModal();
    } catch (err: any) {
      this.errorMessage.set(err.message || 'حدث خطأ أثناء إلغاء المعاملة');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  closeModal(): void {
    this.reason.set('');
    this.errorMessage.set(null);
    this.close.emit();
  }
}
