import { Component, Input, Output, EventEmitter, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StudentBillingSummary, PaymentMethod, PaymentAllocation } from '../../../../../core/models/finance.models';
import { FinanceStore } from '../../../../../core/stores/finance.store';

@Component({
  selector: 'app-record-payment-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './record-payment-modal.component.html',
  styleUrls: ['./record-payment-modal.component.scss']
})
export class RecordPaymentModalComponent {
  private financeStore = inject(FinanceStore);

  @Input() student: StudentBillingSummary | null = null;
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() paymentSuccess = new EventEmitter<any>();

  amountEgp = signal<number>(0);
  method = signal<PaymentMethod>('Cash');
  referenceNumber = signal<string>('');
  notes = signal<string>('');
  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  // Computed FIFO preview
  readonly fifoPreview = computed(() => {
    const s = this.student;
    const amount = Number(this.amountEgp()) || 0;
    if (!s || amount <= 0) return { allocations: [], remainingBalance: 0, surplusCredit: 0 };

    let unallocated = amount;
    const allocations: Array<{ title: string; dueAt: string; chargeAmount: number; allocatedAmount: number }> = [];

    for (const charge of s.openCharges || []) {
      if (unallocated <= 0) break;
      const chargeRem = charge.amount - charge.allocatedAmount;
      if (chargeRem <= 0) continue;

      const take = Math.min(unallocated, chargeRem);
      allocations.push({
        title: charge.titleAr || charge.description || 'مصروفات دراسية',
        dueAt: charge.dueAt || charge.dueAtUtc || '',
        chargeAmount: charge.amount,
        allocatedAmount: take
      });
      unallocated -= take;
    }

    const newRemainingFee = Math.max(0, s.remainingFee - (amount - unallocated));
    const surplusCredit = unallocated;

    return {
      allocations,
      remainingBalance: newRemainingFee,
      surplusCredit
    };
  });

  ngOnChanges(): void {
    if (this.student) {
      this.amountEgp.set(this.student.remainingFee > 0 ? this.student.remainingFee : this.student.requiredFee);
      this.errorMessage.set(null);
    }
  }

  async submitPayment(): Promise<void> {
    if (!this.student) return;
    const amount = Number(this.amountEgp());
    if (amount <= 0) {
      this.errorMessage.set('يرجى إدخال مبلغ صحيح أكبر من الصفر');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    try {
      const res = await this.financeStore.recordPayment({
        studentId: this.student.studentId,
        amount: amount,
        amountEgp: amount,
        paymentMethod: this.method(),
        referenceNumber: this.referenceNumber() || undefined,
        transactionReference: this.referenceNumber() || undefined,
        notes: this.notes() || undefined
      });

      this.paymentSuccess.emit(res);
      this.closeModal();
    } catch (err: any) {
      this.errorMessage.set(err.message || 'حدث خطأ أثناء تسجيل السداد');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  closeModal(): void {
    this.errorMessage.set(null);
    this.close.emit();
  }
}
