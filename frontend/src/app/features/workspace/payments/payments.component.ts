import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FinanceStore } from '../../../core/stores/finance.store';
import { StudentBillingSummary, FinancialTransaction } from '../../../core/models/finance.models';
import { RecordPaymentModalComponent } from './components/record-payment-modal/record-payment-modal.component';
import { PrintableReceiptModalComponent } from './components/printable-receipt-modal/printable-receipt-modal.component';
import { PaymentReversalModalComponent } from './components/payment-reversal-modal/payment-reversal-modal.component';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RecordPaymentModalComponent,
    PrintableReceiptModalComponent,
    PaymentReversalModalComponent
  ],
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.component.scss']
})
export class PaymentsComponent implements OnInit {
  readonly financeStore = inject(FinanceStore);

  readonly activeTab = signal<'roster' | 'ledger'>('roster');
  readonly searchQuery = signal<string>('');
  readonly statusFilter = signal<string>('all');
  readonly gradeFilter = signal<string>('all');

  // Modal states
  readonly selectedStudent = signal<StudentBillingSummary | null>(null);
  readonly showRecordModal = signal<boolean>(false);

  readonly selectedTransaction = signal<FinancialTransaction | null>(null);
  readonly showReceiptModal = signal<boolean>(false);

  readonly reversalTarget = signal<FinancialTransaction | null>(null);
  readonly showReversalModal = signal<boolean>(false);

  // Filtered Roster
  readonly filteredRoster = computed(() => {
    const q = this.searchQuery().trim().toLowerCase();
    const st = this.statusFilter();
    const gr = this.gradeFilter();

    return this.financeStore.billingRoster().filter(item => {
      if (st !== 'all' && item.paymentStatus !== st) return false;
      if (gr !== 'all' && item.gradeName !== gr) return false;
      if (q) {
        const matchesName = item.studentName.toLowerCase().includes(q);
        const matchesCode = item.studentCode.toLowerCase().includes(q);
        if (!matchesName && !matchesCode) return false;
      }
      return true;
    });
  });

  // Filtered Ledger
  readonly filteredLedger = computed(() => {
    const q = this.searchQuery().trim().toLowerCase();
    return this.financeStore.transactionsLedger().filter(t => {
      if (q) {
        const matchName = t.studentName.toLowerCase().includes(q);
        const matchCode = t.studentCode.toLowerCase().includes(q);
        const matchRec = t.receiptNumber?.toLowerCase().includes(q);
        if (!matchName && !matchCode && !matchRec) return false;
      }
      return true;
    });
  });

  ngOnInit(): void {
    this.financeStore.loadBillingData();
  }

  openRecordPayment(student: StudentBillingSummary): void {
    this.selectedStudent.set(student);
    this.showRecordModal.set(true);
  }

  openReceipt(transaction: FinancialTransaction): void {
    this.selectedTransaction.set(transaction);
    this.showReceiptModal.set(true);
  }

  openReversal(transaction: FinancialTransaction): void {
    this.reversalTarget.set(transaction);
    this.showReversalModal.set(true);
  }

  onPaymentRecorded(res: any): void {
    // If a payment was recorded, we can immediately offer to print the receipt
    const tx: FinancialTransaction = {
      id: res.paymentId,
      receiptNumber: res.receiptNumber,
      studentId: res.studentId,
      studentName: this.selectedStudent()?.studentName || '',
      studentCode: this.selectedStudent()?.studentCode || '',
      amount: res.amountPaid,
      type: 'Payment',
      status: 'Completed',
      paymentMethod: res.paymentMethod,
      createdAt: res.recordedAt
    };
    this.openReceipt(tx);
  }
}
