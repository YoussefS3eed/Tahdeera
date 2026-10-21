import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminStore } from '../../../core/stores/admin.store';
import { PlatformSubscriptionPayment, PlatformPaymentMethod } from '../../../core/models/admin.models';
import { RecordPlatformPaymentRequest, ReversePlatformPaymentRequest } from '../../../core/models/admin.requests';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

@Component({
  selector: 'app-revenue',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  template: `
    <div class="revenue-container">
      <header class="page-header">
        <div class="header-main">
          <div>
            <h2 class="page-title">{{ 'ADMIN_REVENUE.PAGE_TITLE' | translate }}</h2>
            <p class="page-subtitle">{{ 'ADMIN_REVENUE.SUBTITLE' | translate }}</p>
          </div>
          <button type="button" class="btn btn-primary" (click)="showRecordModal = true">
            + {{ 'ADMIN_REVENUE.RECORD_PAYMENT_BTN' | translate }}
          </button>
        </div>
      </header>

      <!-- 3 Revenue KPIs -->
      <div class="kpi-grid">
        <div class="kpi-card highlight">
          <span class="kpi-title">{{ 'ADMIN_REVENUE.KPI_COLLECTED' | translate }}</span>
          <div class="kpi-value ltr-num font-mono">{{ totalCollectedEgp }} <span class="currency">ج.م</span></div>
          <span class="kpi-sub">المعاملات المكتملة</span>
        </div>
        <div class="kpi-card">
          <span class="kpi-title">عدد العمليات المسجلة</span>
          <div class="kpi-value ltr-num font-mono">{{ adminStore.subscriptionPayments().length }}</div>
          <span class="kpi-sub">إجمالي قيود الدفتر</span>
        </div>
        <div class="kpi-card alert">
          <span class="kpi-title">المبالغ المعكوسة</span>
          <div class="kpi-value ltr-num font-mono">{{ totalReversedEgp }} <span class="currency">ج.م</span></div>
          <span class="kpi-sub text-danger">تسويات واستردادات</span>
        </div>
      </div>

      <!-- Ledger Table -->
      <div class="section-card">
        <div class="table-responsive">
          <table class="data-table">
            <thead>
              <tr>
                <th>{{ 'ADMIN_REVENUE.REF' | translate }}</th>
                <th>{{ 'ADMIN_REVENUE.COL_WORKSPACE' | translate }}</th>
                <th>{{ 'ADMIN_REVENUE.COL_AMOUNT' | translate }}</th>
                <th>{{ 'ADMIN_REVENUE.COL_METHOD' | translate }}</th>
                <th>{{ 'ADMIN_REVENUE.COL_REF' | translate }}</th>
                <th>تاريخ التحصيل</th>
                <th>{{ 'ADMIN_REVENUE.COL_STATUS' | translate }}</th>
                <th>{{ 'ADMIN_OVERVIEW.COL_ACTION' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (pay of adminStore.subscriptionPayments(); track pay.id) {
                <tr [class.reversed-row]="pay.status === 'Reversed'">
                  <td class="font-mono ltr-num">{{ pay.receiptNumber }}</td>
                  <td><strong>{{ pay.workspaceName }}</strong></td>
                  <td class="font-mono ltr-num font-bold">{{ pay.amount }} ج.م</td>
                  <td>
                    <span class="method-tag">{{ getMethodLabel(pay.method) }}</span>
                  </td>
                  <td class="font-mono ltr-num">{{ pay.referenceNumber || '—' }}</td>
                  <td class="font-mono ltr-num">{{ pay.paidAt | date:'yyyy/MM/dd HH:mm' }}</td>
                  <td>
                    @if (pay.status === 'Completed') {
                      <span class="badge badge-success">مكتمل</span>
                    } @else {
                      <div class="reversed-cell">
                        <span class="badge badge-danger">معكوس</span>
                        <span class="reversal-reason" [title]="pay.reversalReason || ''">{{ pay.reversalReason }}</span>
                      </div>
                    }
                  </td>
                  <td>
                    @if (pay.status === 'Completed') {
                      <button type="button" class="btn btn-sm btn-outline-danger" (click)="openReverseModal(pay)">
                        {{ 'ADMIN_REVENUE.REVERSE_PAYMENT_BTN' | translate }}
                      </button>
                    } @else {
                      <span class="text-muted text-sm font-mono">معكوسة ومغلقة</span>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>

      <!-- Record Payment Modal -->
      @if (showRecordModal) {
        <div class="modal-backdrop">
          <div class="modal-card">
            <h3 class="modal-title">{{ 'ADMIN_REVENUE.RECORD_PAYMENT_TITLE' | translate }}</h3>
            <form (ngSubmit)="submitRecordPayment()">
              <div class="form-group">
                <label>{{ 'ADMIN_REVENUE.COL_WORKSPACE' | translate }} *</label>
                <select [(ngModel)]="newPayment.workspaceId" name="workspaceId" required>
                  <option value="">اختر السنتر...</option>
                  @for (ws of adminStore.workspaces(); track ws.id) {
                    <option [value]="ws.id">{{ ws.name }} ({{ ws.code }})</option>
                  }
                </select>
              </div>

              <div class="form-row">
                <div class="form-group">
                  <label>{{ 'ADMIN_REVENUE.COL_AMOUNT' | translate }} (ج.م) *</label>
                  <input type="number" [(ngModel)]="newPayment.amount" name="amount" required min="1" class="ltr-num font-mono" />
                </div>
                <div class="form-group">
                  <label>{{ 'ADMIN_REVENUE.COL_METHOD' | translate }} *</label>
                  <select [(ngModel)]="newPayment.method" name="method" required>
                    <option value="InstaPay">إنستاباي (InstaPay)</option>
                    <option value="VodafoneCash">فودافون كاش</option>
                    <option value="BankTransfer">تحويل بنكي</option>
                    <option value="Cash">نقدي</option>
                  </select>
                </div>
              </div>

              <div class="form-group">
                <label>{{ 'ADMIN_REVENUE.COL_REF' | translate }} *</label>
                <input type="text" [(ngModel)]="newPayment.referenceNumber" name="referenceNumber" required placeholder="رقم المعاملة البنكية أو كود إنستاباي" class="font-mono" />
              </div>

              <div class="modal-actions">
                <button type="submit" class="btn btn-primary" [disabled]="!newPayment.workspaceId || !newPayment.amount">
                  {{ 'ADMIN_REVENUE.RECORD_PAYMENT_BTN' | translate }}
                </button>
                <button type="button" class="btn btn-secondary" (click)="showRecordModal = false">{{ 'common.cancel' | translate }}</button>
              </div>
            </form>
          </div>
        </div>
      }

      <!-- Immutable Reversal Modal (DEC-04) -->
      @if (targetPaymentForReversal) {
        <div class="modal-backdrop">
          <div class="modal-card">
            <h3 class="modal-title text-danger">{{ 'ADMIN_REVENUE.REVERSAL_MODAL_TITLE' | translate }}</h3>
            <p class="modal-desc">
              سيتم قيد عملية عكس محاسبية غير قابلة للحذف للإيصال رقم:
              <strong class="ltr-num font-mono">{{ targetPaymentForReversal.receiptNumber }}</strong> بقيمة
              <strong class="ltr-num font-mono">{{ targetPaymentForReversal.amount }} ج.م</strong>.
            </p>
            <div class="form-group">
              <label>{{ 'ADMIN_REVENUE.REVERSAL_REASON_LABEL' | translate }}</label>
              <textarea
                [(ngModel)]="reversalReason"
                rows="3"
                placeholder="اكتب سبب عكس الدفعة بالتفصيل للتسوية المحاسبية..."
              ></textarea>
            </div>
            <div class="modal-actions">
              <button
                type="button"
                class="btn btn-danger"
                [disabled]="!reversalReason || reversalReason.trim().length < 5"
                (click)="confirmReversal()"
              >
                {{ 'ADMIN_REVENUE.CONFIRM_REVERSE_BTN' | translate }}
              </button>
              <button type="button" class="btn btn-secondary" (click)="targetPaymentForReversal = null">{{ 'common.cancel' | translate }}</button>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .revenue-container {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }
    .page-header {
      margin-block-end: 0.5rem;
    }
    .header-main {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
    }
    .page-title {
      font-size: 1.5rem;
      font-weight: 700;
      color: #F8FAFC;
      margin-block-end: 0.25rem;
    }
    .page-subtitle {
      font-size: 0.95rem;
      color: #94A3B8;
    }
    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 1.25rem;
    }
    .kpi-card {
      background: #0B2244;
      border: 1px solid #1E3A5F;
      border-radius: 0.75rem;
      padding-block: 1.25rem;
      padding-inline: 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
    }
    .kpi-card.highlight { border-color: #059669; }
    .kpi-card.alert { border-color: #EF4444; }
    .kpi-title { font-size: 0.85rem; color: #94A3B8; }
    .kpi-value { font-size: 1.75rem; font-weight: 700; color: #F8FAFC; }
    .currency { font-size: 0.95rem; color: #94A3B8; }
    .kpi-sub { font-size: 0.8rem; color: #64748B; }
    .text-danger { color: #EF4444; }
    .section-card {
      background: #0B2244;
      border: 1px solid #1E3A5F;
      border-radius: 0.75rem;
      padding-block: 1.25rem;
      padding-inline: 1.25rem;
    }
    .table-responsive { overflow-x: auto; }
    .data-table {
      inline-size: 100%;
      border-collapse: collapse;
      text-align: start;
      font-size: 0.9rem;
    }
    .data-table th {
      padding-block: 0.75rem;
      padding-inline: 0.75rem;
      background: #081A35;
      color: #94A3B8;
      font-weight: 500;
      border-block-end: 1px solid #1E3A5F;
    }
    .data-table td {
      padding-block: 0.85rem;
      padding-inline: 0.75rem;
      border-block-end: 1px solid #132D50;
      color: #CBD5E1;
    }
    .reversed-row { opacity: 0.65; background: rgba(239, 68, 68, 0.05); }
    .font-bold { font-weight: 700; }
    .method-tag {
      padding-block: 0.15rem;
      padding-inline: 0.5rem;
      background: #1E293B;
      color: #38BDF8;
      border-radius: 0.25rem;
      font-size: 0.8rem;
    }
    .badge {
      display: inline-block;
      padding-block: 0.2rem;
      padding-inline: 0.6rem;
      border-radius: 9999px;
      font-size: 0.8rem;
      font-weight: 600;
    }
    .badge-success { background: rgba(16, 185, 129, 0.2); color: #34D399; }
    .badge-danger { background: rgba(239, 68, 68, 0.2); color: #F87171; }
    .reversed-cell { display: flex; flex-direction: column; gap: 0.2rem; }
    .reversal-reason { font-size: 0.75rem; color: #EF4444; max-inline-size: 12rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .btn {
      padding-block: 0.5rem;
      padding-inline: 1rem;
      border-radius: 0.375rem;
      font-weight: 600;
      font-size: 0.85rem;
      cursor: pointer;
      border: 1px solid transparent;
    }
    .btn-sm { padding-block: 0.3rem; padding-inline: 0.65rem; }
    .btn-primary { background: #0284C7; color: #FFFFFF; }
    .btn-secondary { background: #334155; color: #F8FAFC; }
    .btn-danger { background: #DC2626; color: #FFFFFF; }
    .btn-outline-danger { background: transparent; border-color: #EF4444; color: #EF4444; }
    .btn-outline-danger:hover { background: rgba(239, 68, 68, 0.1); }
    .modal-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.75);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 1rem;
    }
    .modal-card {
      background: #0B2244;
      border: 1px solid #1E3A5F;
      border-radius: 0.75rem;
      padding-block: 2rem;
      padding-inline: 2rem;
      inline-size: 100%;
      max-inline-size: 34rem;
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }
    .modal-title { font-size: 1.25rem; font-weight: 700; color: #F8FAFC; margin: 0; }
    .modal-desc { font-size: 0.9rem; color: #CBD5E1; margin: 0; line-height: 1.5; }
    .form-group { display: flex; flex-direction: column; gap: 0.4rem; }
    .form-group label { font-size: 0.85rem; color: #94A3B8; }
    .form-group input, .form-group select, .form-group textarea {
      padding-block: 0.6rem;
      padding-inline: 0.85rem;
      background: #081A35;
      border: 1px solid #1E3A5F;
      border-radius: 0.375rem;
      color: #F8FAFC;
      font-size: 0.9rem;
    }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .modal-actions { display: flex; justify-content: flex-end; gap: 0.75rem; margin-block-start: 1rem; }
  `]
})
export class RevenueComponent implements OnInit {
  readonly adminStore = inject(AdminStore);

  showRecordModal = false;
  targetPaymentForReversal: PlatformSubscriptionPayment | null = null;
  reversalReason = '';

  newPayment: RecordPlatformPaymentRequest = {
    workspaceId: '',
    amount: 1500,
    method: 'InstaPay',
    referenceNumber: ''
  };

  ngOnInit(): void {
    this.adminStore.loadPayments().subscribe();
    this.adminStore.loadWorkspaces().subscribe();
  }

  get totalCollectedEgp(): number {
    return this.adminStore.subscriptionPayments()
      .filter(p => p.status === 'Completed')
      .reduce((sum, p) => sum + p.amount, 0);
  }

  get totalReversedEgp(): number {
    return this.adminStore.subscriptionPayments()
      .filter(p => p.status === 'Reversed')
      .reduce((sum, p) => sum + p.amount, 0);
  }

  getMethodLabel(method: PlatformPaymentMethod): string {
    switch (method) {
      case 'InstaPay': return 'إنستاباي';
      case 'VodafoneCash': return 'فودافون كاش';
      case 'BankTransfer': return 'تحويل بنكي';
      case 'Cash': return 'نقدي';
    }
  }

  submitRecordPayment(): void {
    this.adminStore.recordPayment(this.newPayment).subscribe(() => {
      this.showRecordModal = false;
      this.newPayment = { workspaceId: '', amount: 1500, method: 'InstaPay', referenceNumber: '' };
    });
  }

  openReverseModal(pay: PlatformSubscriptionPayment): void {
    this.targetPaymentForReversal = pay;
    this.reversalReason = '';
  }

  confirmReversal(): void {
    if (!this.targetPaymentForReversal || this.reversalReason.trim().length < 5) return;
    this.adminStore.reversePayment(this.targetPaymentForReversal.id, { reversalReason: this.reversalReason.trim() }).subscribe(() => {
      this.targetPaymentForReversal = null;
    });
  }
}
