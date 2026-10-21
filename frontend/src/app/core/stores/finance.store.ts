import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  StudentBillingSummary,
  FinancialTransaction,
  FinancialKpis,
  PaymentMethod
} from '../models/finance.models';

export interface RecordPaymentDto {
  studentId: string;
  amount?: number;
  amountEgp?: number;
  paymentMethod: PaymentMethod;
  transactionReference?: string;
  referenceNumber?: string;
  notes?: string;
}

@Injectable({ providedIn: 'root' })
export class FinanceStore {
  private http = inject(HttpClient);

  readonly billingRoster = signal<StudentBillingSummary[]>([]);
  readonly transactionsLedger = signal<FinancialTransaction[]>([]);
  readonly kpis = signal<FinancialKpis | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly filterSearch = signal<string>('');
  readonly filterStatus = signal<string>('all');
  readonly selectedStudentForPayment = signal<StudentBillingSummary | null>(null);
  readonly activeReceipt = signal<FinancialTransaction | null>(null);

  readonly filteredStudents = computed(() => {
    const q = this.filterSearch().trim().toLowerCase();
    const st = this.filterStatus();
    return this.billingRoster().filter(s => {
      if (q && !s.studentName.toLowerCase().includes(q) && !s.studentCode.toLowerCase().includes(q)) {
        return false;
      }
      if (st !== 'all' && s.paymentStatus !== st) {
        return false;
      }
      return true;
    });
  });

  async loadBillingData(): Promise<void> {
    this.isLoading.set(true);
    try {
      const [summaryRes, ledgerRes]: any = await Promise.all([
        firstValueFrom(this.http.get('/api/v1/workspaces/ws-main/payments/summary')),
        firstValueFrom(this.http.get('/api/v1/workspaces/ws-main/payments/ledger'))
      ]);
      this.billingRoster.set(summaryRes.students || []);
      this.kpis.set(summaryRes.kpis || null);
      this.transactionsLedger.set(ledgerRes.transactions || []);
    } finally {
      this.isLoading.set(false);
    }
  }

  async recordPayment(dto: RecordPaymentDto): Promise<FinancialTransaction> {
    this.isLoading.set(true);
    const effectiveAmount = dto.amount ?? dto.amountEgp ?? 0;
    const payload = {
      studentId: dto.studentId,
      amount: effectiveAmount,
      amountEgp: effectiveAmount,
      paymentMethod: dto.paymentMethod,
      transactionReference: dto.transactionReference || dto.referenceNumber || '',
      referenceNumber: dto.referenceNumber || dto.transactionReference || '',
      notes: dto.notes
    };
    try {
      const tx = await firstValueFrom(
        this.http.post<FinancialTransaction>('/api/v1/workspaces/ws-main/payments', payload)
      );

      // Prepend to transactions ledger
      this.transactionsLedger.update(list => [tx, ...list]);

      // Update student in billing roster
      this.billingRoster.update(list =>
        list.map(s => {
          if (s.studentId === dto.studentId) {
            const paid = s.paidAmount + effectiveAmount;
            const remaining = Math.max(0, s.requiredFee - paid);
            return {
              ...s,
              paidAmount: paid,
              remainingFee: remaining,
              paymentStatus: remaining === 0 ? 'Paid' : (paid > 0 ? 'Partial' : 'Unpaid'),
              lastPaymentDate: tx.recordedAtUtc
            };
          }
          return s;
        })
      );

      // Recalculate KPIs
      this.kpis.update(k => {
        if (!k) return null;
        return {
          ...k,
          totalCollectedEgp: k.totalCollectedEgp + effectiveAmount,
          totalRemainingEgp: Math.max(0, k.totalRemainingEgp - effectiveAmount)
        };
      });

      this.activeReceipt.set(tx);
      return tx;
    } finally {
      this.isLoading.set(false);
    }
  }

  async reversePayment(paymentId: string, reason: string): Promise<FinancialTransaction> {
    this.isLoading.set(true);
    try {
      const tx = await firstValueFrom(
        this.http.post<FinancialTransaction>(
          `/api/v1/workspaces/ws-main/payments/${paymentId}/reverse`,
          { reversalReason: reason }
        )
      );

      // Update in transaction ledger
      this.transactionsLedger.update(list =>
        list.map(t => (t.id === paymentId ? tx : t))
      );

      // Restore student balance
      this.billingRoster.update(list =>
        list.map(s => {
          if (s.studentId === tx.studentId) {
            const paid = Math.max(0, s.paidAmount - tx.amount);
            const remaining = Math.max(0, s.requiredFee - paid);
            return {
              ...s,
              paidAmount: paid,
              remainingFee: remaining,
              paymentStatus: remaining === 0 ? 'Paid' : (paid > 0 ? 'Partial' : 'Unpaid')
            };
          }
          return s;
        })
      );

      // Recalculate KPIs
      this.kpis.update(k => {
        if (!k) return null;
        return {
          ...k,
          totalCollectedEgp: Math.max(0, k.totalCollectedEgp - tx.amount),
          totalRemainingEgp: k.totalRemainingEgp + tx.amount
        };
      });

      return tx;
    } finally {
      this.isLoading.set(false);
    }
  }
}
