import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminStore } from '../../../core/stores/admin.store';
import { AdminAuditLogEntry } from '../../../core/models/admin.models';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  template: `
    <div class="audit-container">
      <header class="page-header">
        <h2 class="page-title">{{ 'ADMIN_AUDIT.PAGE_TITLE' | translate }}</h2>
        <p class="page-subtitle">{{ 'ADMIN_AUDIT.SUBTITLE' | translate }}</p>
      </header>

      <!-- Filter Bar -->
      <div class="filter-bar">
        <input
          type="text"
          class="search-input"
          [placeholder]="'ADMIN_AUDIT.SEARCH_PLACEHOLDER' | translate"
          [(ngModel)]="searchQuery"
          (ngModelChange)="applyFilter()"
        />
        <select class="action-select" [(ngModel)]="actionFilter" (ngModelChange)="applyFilter()">
          <option value="">جميع العمليات</option>
          <option value="CREATE_WORKSPACE">إنشاء سنتر (CREATE_WORKSPACE)</option>
          <option value="SUSPEND_WORKSPACE">إيقاف سنتر (SUSPEND_WORKSPACE)</option>
          <option value="REACTIVE_WORKSPACE">تفعيل سنتر (REACTIVE_WORKSPACE)</option>
          <option value="RENEW_SUBSCRIPTION">تجديد اشتراك (RENEW_SUBSCRIPTION)</option>
          <option value="UPDATE_PACKAGE_OVERRIDES">تعديل ميزات (UPDATE_PACKAGE_OVERRIDES)</option>
          <option value="RECORD_PAYMENT">تسجيل دفعة (RECORD_PAYMENT)</option>
          <option value="REVERSE_PAYMENT">عكس معاملة (REVERSE_PAYMENT)</option>
          <option value="ADMIN_IMPERSONATION_STARTED">بدء معايشة سنتر (IMPERSONATION_STARTED)</option>
          <option value="ADMIN_IMPERSONATION_ENDED">إنهاء معايشة سنتر (IMPERSONATION_ENDED)</option>
        </select>
      </div>

      <!-- Audit Table (Strictly NO delete or purge buttons) -->
      <div class="section-card">
        <div class="table-responsive">
          <table class="data-table">
            <thead>
              <tr>
                <th>التاريخ والوقت</th>
                <th>{{ 'ADMIN_AUDIT.ACTION' | translate }}</th>
                <th>{{ 'ADMIN_AUDIT.ACTOR' | translate }}</th>
                <th>{{ 'ADMIN_OVERVIEW.COL_CENTER' | translate }}</th>
                <th>فحص الفروقات (Diff)</th>
              </tr>
            </thead>
            <tbody>
              @for (entry of filteredLogs; track entry.id) {
                <tr>
                  <td class="font-mono ltr-num">{{ entry.createdAt | date:'yyyy/MM/dd HH:mm:ss' }}</td>
                  <td>
                    <span class="action-tag font-mono">{{ entry.action }}</span>
                  </td>
                  <td>
                    <strong>{{ entry.adminName }}</strong>
                  </td>
                  <td>
                    <span class="target-name">{{ entry.targetName }}</span>
                    <span class="target-type font-mono">({{ entry.targetType }})</span>
                  </td>
                  <td>
                    @if (entry.beforeState || entry.afterState) {
                      <button type="button" class="btn btn-sm btn-outline-info" (click)="openDiffModal(entry)">
                        🔍 فحص الفروقات (Diff)
                      </button>
                    } @else {
                      <span class="text-muted text-sm">—</span>
                    }
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="5" class="empty-state">لا توجد عمليات مطابقة لمعايير البحث.</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>

      <!-- JSONB Diff Modal -->
      @if (selectedEntry) {
        <div class="modal-backdrop">
          <div class="modal-card">
            <h3 class="modal-title">{{ 'ADMIN_AUDIT.DIFF_MODAL_TITLE' | translate }}: {{ selectedEntry.action }}</h3>
            <p class="modal-subtitle">الهدف: {{ selectedEntry.targetName }} | المسؤول: {{ selectedEntry.adminName }}</p>

            <div class="diff-grid">
              <div class="diff-panel before">
                <div class="diff-header text-danger">{{ 'ADMIN_AUDIT.BEFORE_LABEL' | translate }}</div>
                <pre class="diff-code">{{ formatJson(selectedEntry.beforeState) }}</pre>
              </div>
              <div class="diff-panel after">
                <div class="diff-header text-success">{{ 'ADMIN_AUDIT.AFTER_LABEL' | translate }}</div>
                <pre class="diff-code">{{ formatJson(selectedEntry.afterState) }}</pre>
              </div>
            </div>

            <div class="modal-actions">
              <button type="button" class="btn btn-secondary" (click)="selectedEntry = null">{{ 'common.close' | translate }}</button>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .audit-container {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }
    .page-header {
      margin-block-end: 0.5rem;
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
    .filter-bar {
      display: flex;
      gap: 1rem;
    }
    .search-input {
      flex: 1;
      padding-block: 0.65rem;
      padding-inline: 1rem;
      background: #0B2244;
      border: 1px solid #1E3A5F;
      border-radius: 0.5rem;
      color: #F8FAFC;
      font-size: 0.9rem;
    }
    .action-select {
      padding-block: 0.65rem;
      padding-inline: 1rem;
      background: #0B2244;
      border: 1px solid #1E3A5F;
      border-radius: 0.5rem;
      color: #F8FAFC;
      font-size: 0.9rem;
    }
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
    .action-tag {
      padding-block: 0.2rem;
      padding-inline: 0.5rem;
      background: #1E293B;
      color: #38BDF8;
      border-radius: 0.25rem;
      font-size: 0.8rem;
    }
    .target-name { font-weight: 600; color: #F8FAFC; }
    .target-type { font-size: 0.75rem; color: #64748B; margin-inline-start: 0.35rem; }
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
    .btn-outline-info { background: transparent; border-color: #0284C7; color: #38BDF8; }
    .btn-outline-info:hover { background: rgba(56, 189, 248, 0.1); }
    .btn-secondary { background: #334155; color: #F8FAFC; }
    .empty-state { text-align: center; padding: 2rem; color: #94A3B8; }
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
      max-inline-size: 42rem;
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }
    .modal-title { font-size: 1.25rem; font-weight: 700; color: #F8FAFC; margin: 0; }
    .modal-subtitle { font-size: 0.85rem; color: #94A3B8; margin: 0; }
    .diff-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }
    .diff-panel {
      background: #061329;
      border: 1px solid #1E3A5F;
      border-radius: 0.5rem;
      padding: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }
    .diff-panel.before { border-color: rgba(239, 68, 68, 0.4); }
    .diff-panel.after { border-color: rgba(16, 185, 129, 0.4); }
    .diff-header { font-size: 0.85rem; font-weight: 600; }
    .text-danger { color: #EF4444; }
    .text-success { color: #10B981; }
    .diff-code {
      font-family: monospace;
      font-size: 0.8rem;
      color: #E2E8F0;
      white-space: pre-wrap;
      margin: 0;
      max-block-size: 16rem;
      overflow-y: auto;
    }
    .modal-actions { display: flex; justify-content: flex-end; margin-block-start: 0.5rem; }
  `]
})
export class AuditLogComponent implements OnInit {
  readonly adminStore = inject(AdminStore);

  searchQuery = '';
  actionFilter = '';
  selectedEntry: AdminAuditLogEntry | null = null;

  ngOnInit(): void {
    this.adminStore.loadAuditLogs().subscribe();
  }

  get filteredLogs(): AdminAuditLogEntry[] {
    return this.adminStore.auditLogs().filter(entry => {
      const matchSearch = !this.searchQuery ||
        (entry.targetName || '').includes(this.searchQuery) ||
        (entry.adminName || '').includes(this.searchQuery) ||
        (entry.action || '').toLowerCase().includes(this.searchQuery.toLowerCase());
      const matchAction = !this.actionFilter || entry.action === this.actionFilter;
      return matchSearch && matchAction;
    });
  }

  applyFilter(): void {}

  openDiffModal(entry: AdminAuditLogEntry): void {
    this.selectedEntry = entry;
  }

  formatJson(obj: any): string {
    if (!obj) return '— لا توجد بيانات —';
    return JSON.stringify(obj, null, 2);
  }
}
