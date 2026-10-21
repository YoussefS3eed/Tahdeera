import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminStore } from '../../../core/stores/admin.store';
import { AdminImpersonationStore } from '../../../core/stores/admin-impersonation.store';
import { AdminWorkspaceSummary } from '../../../core/models/admin.models';
import { CreateWorkspaceRequest } from '../../../core/models/admin.requests';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  template: `
    <div class="accounts-container">
      <header class="page-header">
        <div class="header-main">
          <div>
            <h2 class="page-title">{{ 'ADMIN_ACCOUNTS.PAGE_TITLE' | translate }}</h2>
            <p class="page-subtitle">{{ 'ADMIN_ACCOUNTS.SUBTITLE' | translate }}</p>
          </div>
          <button type="button" class="btn btn-primary" (click)="openCreateModal()">
            + {{ 'ADMIN_ACCOUNTS.MODAL_NEW_TITLE' | translate }}
          </button>
        </div>
      </header>

      <!-- Filter Bar -->
      <div class="filter-bar">
        <input
          type="text"
          class="search-input"
          [placeholder]="'ADMIN_ACCOUNTS.SEARCH_PLACEHOLDER' | translate"
          [(ngModel)]="searchQuery"
          (ngModelChange)="applyFilter()"
        />
        <select class="status-select" [(ngModel)]="statusFilter" (ngModelChange)="applyFilter()">
          <option value="">{{ 'ADMIN_ACCOUNTS.FILTER_ALL' | translate }}</option>
          <option value="Active">{{ 'ADMIN_ACCOUNTS.FILTER_ACTIVE' | translate }}</option>
          <option value="Suspended">{{ 'ADMIN_ACCOUNTS.FILTER_SUSPENDED' | translate }}</option>
        </select>
      </div>

      <!-- Accounts Table -->
      <div class="section-card">
        <div class="table-responsive">
          <table class="data-table">
            <thead>
              <tr>
                <th>{{ 'ADMIN_ACCOUNTS.COL_NAME_CODE' | translate }}</th>
                <th>{{ 'ADMIN_ACCOUNTS.COL_TYPE' | translate }}</th>
                <th>{{ 'ADMIN_ACCOUNTS.COL_OWNER' | translate }}</th>
                <th>{{ 'ADMIN_ACCOUNTS.COL_STUDENTS' | translate }}</th>
                <th>{{ 'ADMIN_ACCOUNTS.COL_ACC_STATUS' | translate }}</th>
                <th>{{ 'ADMIN_ACCOUNTS.COL_SUB_STATUS' | translate }}</th>
                <th>{{ 'ADMIN_ACCOUNTS.COL_ACTIONS' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (ws of filteredWorkspaces; track ws.id) {
                <tr>
                  <td>
                    <div class="workspace-info">
                      <strong class="ws-name">{{ ws.name }}</strong>
                      <span class="code-badge ltr-num font-mono">{{ ws.code }}</span>
                    </div>
                  </td>
                  <td>
                    <span class="type-badge" [class.center]="ws.type === 'Center'">
                      {{ ws.type === 'Center' ? ('ADMIN_ACCOUNTS.TYPE_CENTER' | translate) : ('ADMIN_ACCOUNTS.TYPE_TEACHER' | translate) }}
                    </span>
                  </td>
                  <td>
                    <div class="owner-cell">
                      <span>{{ ws.ownerName }}</span>
                      <span class="ltr-num font-mono text-muted">{{ ws.ownerPhone }}</span>
                    </div>
                  </td>
                  <td>
                    <div class="usage-cell">
                      <span class="tier-name font-mono">{{ ws.packageTier }}</span>
                      <span class="student-count">
                        <span class="ltr-num font-mono">{{ ws.studentCount }}</span> / <span class="ltr-num font-mono">{{ ws.maxStudentsQuota }}</span> طالب
                      </span>
                    </div>
                  </td>
                  <td>
                    @if (ws.status === 'Active') {
                      <span class="badge badge-success">{{ 'ADMIN_ACCOUNTS.FILTER_ACTIVE' | translate }}</span>
                    } @else {
                      <span class="badge badge-danger">{{ 'ADMIN_ACCOUNTS.FILTER_SUSPENDED' | translate }}</span>
                    }
                  </td>
                  <td>
                    <span class="badge" [class.badge-warning]="(ws.subscriptionDaysRemaining ?? 0) <= 7" [class.badge-success]="(ws.subscriptionDaysRemaining ?? 0) > 7">
                      <span class="ltr-num font-mono">{{ ws.subscriptionDaysRemaining ?? 0 }}</span> يوم
                    </span>
                  </td>
                  <td>
                    <div class="action-buttons">
                      <button
                        type="button"
                        class="btn-action impersonate-btn"
                        title="الدخول بصلاحية مدير السنتر للتجربة والدعم"
                        (click)="impersonate(ws)"
                      >
                        {{ 'ADMIN_ACCOUNTS.ACTION_IMPERSONATE' | translate }}
                      </button>
                      @if (ws.status === 'Active') {
                        <button
                          type="button"
                          class="btn-action suspend-btn"
                          (click)="openSuspendModal(ws)"
                        >
                          {{ 'ADMIN_ACCOUNTS.ACTION_SUSPEND' | translate }}
                        </button>
                      } @else {
                        <button
                          type="button"
                          class="btn-action reactivate-btn"
                          (click)="reactivate(ws)"
                        >
                          {{ 'ADMIN_ACCOUNTS.ACTION_REACTIVATE' | translate }}
                        </button>
                      }
                    </div>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="7" class="empty-state">لا توجد حسابات مطابقة لمعايير البحث.</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>

      <!-- Create Workspace Modal -->
      @if (showCreateModal) {
        <div class="modal-backdrop">
          <div class="modal-card">
            <h3 class="modal-title">{{ 'ADMIN_ACCOUNTS.MODAL_NEW_TITLE' | translate }}</h3>
            <form (ngSubmit)="submitCreate()">
              <div class="form-group">
                <label>{{ 'ADMIN_ACCOUNTS.FIELD_NAME' | translate }} *</label>
                <input type="text" [(ngModel)]="newWorkspace.name" name="name" required minlength="3" placeholder="مثال: سنتر القمة التعليمي" />
              </div>
              <div class="form-row">
                <div class="form-group">
                  <label>{{ 'ADMIN_ACCOUNTS.FIELD_TYPE' | translate }} *</label>
                  <select [(ngModel)]="newWorkspace.type" name="type">
                    <option value="Center">{{ 'ADMIN_ACCOUNTS.TYPE_CENTER' | translate }} (Center)</option>
                    <option value="Teacher">{{ 'ADMIN_ACCOUNTS.TYPE_TEACHER' | translate }} (Teacher)</option>
                  </select>
                </div>
                <div class="form-group">
                  <label>{{ 'ADMIN_ACCOUNTS.FIELD_PACKAGE' | translate }} *</label>
                  <select [(ngModel)]="newWorkspace.packageTier" name="packageTier">
                    <option value="basic">الأساسية (Basic)</option>
                    <option value="pro">المتقدمة (Pro)</option>
                    <option value="center">السناتر الكبرى (Center)</option>
                  </select>
                </div>
              </div>
              <div class="form-row">
                <div class="form-group">
                  <label>{{ 'ADMIN_ACCOUNTS.FIELD_OWNER_NAME' | translate }} *</label>
                  <input type="text" [(ngModel)]="newWorkspace.ownerName" name="ownerName" required placeholder="مثال: أ. أحمد رضوان" />
                </div>
                <div class="form-group">
                  <label>{{ 'ADMIN_ACCOUNTS.FIELD_PHONE' | translate }} *</label>
                  <input type="text" [(ngModel)]="newWorkspace.ownerPhone" name="ownerPhone" required pattern="^01[0125][0-9]{8}$" placeholder="01019482687" class="ltr-num font-mono" />
                </div>
              </div>
              <div class="form-row">
                <div class="form-group">
                  <label>{{ 'ADMIN_ACCOUNTS.FIELD_EMAIL' | translate }} *</label>
                  <input type="email" [(ngModel)]="newWorkspace.ownerEmail" name="ownerEmail" required placeholder="owner@center.com" class="font-mono" />
                </div>
                <div class="form-group">
                  <label>{{ 'ADMIN_ACCOUNTS.FIELD_PRICE' | translate }} *</label>
                  <input type="number" [(ngModel)]="newWorkspace.agreedPrice" name="agreedPrice" required min="0" class="ltr-num font-mono" />
                </div>
              </div>

              <!-- Generated Credential Preview -->
              @if (createdCredentialsCard) {
                <div class="credential-card">
                  <div class="credential-header">
                    <span>{{ 'ADMIN_ACCOUNTS.CREDENTIALS_TITLE' | translate }}</span>
                    <button type="button" class="btn-copy" (click)="copyCredentials()">{{ 'ADMIN_ACCOUNTS.COPY_WHATSAPP_BTN' | translate }}</button>
                  </div>
                  <pre class="credential-text">{{ createdCredentialsCard }}</pre>
                </div>
              }

              <div class="modal-actions">
                @if (!createdCredentialsCard) {
                  <button type="submit" class="btn btn-primary">{{ 'ADMIN_ACCOUNTS.BTN_CREATE' | translate }}</button>
                }
                <button type="button" class="btn btn-secondary" (click)="closeCreateModal()">{{ 'common.close' | translate }}</button>
              </div>
            </form>
          </div>
        </div>
      }

      <!-- Mandatory Suspension Modal (DEC-08) -->
      @if (targetWorkspaceForSuspension) {
        <div class="modal-backdrop">
          <div class="modal-card">
            <h3 class="modal-title text-danger">{{ 'ADMIN_ACCOUNTS.MODAL_SUSPEND_TITLE' | translate }}</h3>
            <p class="modal-desc">
              {{ 'ADMIN_ACCOUNTS.SUSPEND_WARNING' | translate }}
            </p>
            <div class="form-group">
              <label>{{ 'ADMIN_ACCOUNTS.SUSPEND_REASON_LABEL' | translate }}</label>
              <textarea
                [(ngModel)]="suspensionReason"
                rows="3"
                placeholder="اكتب سبب الإيقاف بالتفصيل لحفظه في سجل العمليات (Audit Log)..."
              ></textarea>
            </div>
            <div class="modal-actions">
              <button
                type="button"
                class="btn btn-danger"
                [disabled]="!suspensionReason || suspensionReason.trim().length < 5"
                (click)="confirmSuspension()"
              >
                {{ 'ADMIN_ACCOUNTS.SUSPEND_CONFIRM_BTN' | translate }}
              </button>
              <button type="button" class="btn btn-secondary" (click)="targetWorkspaceForSuspension = null">{{ 'common.cancel' | translate }}</button>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .accounts-container {
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
    .status-select {
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
    .table-responsive {
      overflow-x: auto;
    }
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
    .workspace-info {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }
    .code-badge {
      display: inline-block;
      font-size: 0.75rem;
      background: #1E3A5F;
      color: #38BDF8;
      padding-block: 0.1rem;
      padding-inline: 0.4rem;
      border-radius: 0.25rem;
      inline-size: fit-content;
    }
    .type-badge {
      font-size: 0.8rem;
      padding-block: 0.2rem;
      padding-inline: 0.5rem;
      border-radius: 0.25rem;
      background: #334155;
      color: #E2E8F0;
    }
    .type-badge.center {
      background: #1E293B;
      color: #38BDF8;
      border: 1px solid #0284C7;
    }
    .owner-cell {
      display: flex;
      flex-direction: column;
    }
    .text-muted {
      color: #94A3B8;
      font-size: 0.8rem;
    }
    .usage-cell {
      display: flex;
      flex-direction: column;
    }
    .tier-name {
      font-size: 0.8rem;
      color: #38BDF8;
      text-transform: uppercase;
    }
    .student-count {
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
    .badge-success {
      background: rgba(16, 185, 129, 0.2);
      color: #34D399;
    }
    .badge-danger {
      background: rgba(239, 68, 68, 0.2);
      color: #F87171;
    }
    .badge-warning {
      background: rgba(245, 158, 11, 0.2);
      color: #FBBF24;
    }
    .action-buttons {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }
    .btn-action {
      padding-block: 0.3rem;
      padding-inline: 0.65rem;
      border-radius: 0.375rem;
      font-size: 0.8rem;
      font-weight: 600;
      cursor: pointer;
      border: 1px solid transparent;
      white-space: nowrap;
    }
    .impersonate-btn {
      background: #D97706;
      color: #FFFFFF;
    }
    .impersonate-btn:hover {
      background: #B45309;
    }
    .suspend-btn {
      background: transparent;
      color: #EF4444;
      border-color: #EF4444;
    }
    .suspend-btn:hover {
      background: rgba(239, 68, 68, 0.1);
    }
    .reactivate-btn {
      background: transparent;
      color: #10B981;
      border-color: #10B981;
    }
    .reactivate-btn:hover {
      background: rgba(16, 185, 129, 0.1);
    }
    .empty-state {
      text-align: center;
      padding-block: 2rem;
      color: #94A3B8;
    }
    .btn {
      padding-block: 0.65rem;
      padding-inline: 1.25rem;
      border-radius: 0.5rem;
      font-weight: 600;
      font-size: 0.9rem;
      cursor: pointer;
      border: none;
    }
    .btn-primary {
      background: #0284C7;
      color: #FFFFFF;
    }
    .btn-primary:hover {
      background: #0369A1;
    }
    .btn-secondary {
      background: #334155;
      color: #F8FAFC;
    }
    .btn-danger {
      background: #DC2626;
      color: #FFFFFF;
    }
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
      max-inline-size: 38rem;
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }
    .modal-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: #F8FAFC;
      margin: 0;
    }
    .text-danger { color: #EF4444; }
    .modal-desc {
      font-size: 0.9rem;
      color: #CBD5E1;
      line-height: 1.5;
    }
    .form-group {
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
      margin-block-end: 1rem;
    }
    .form-group label {
      font-size: 0.85rem;
      color: #94A3B8;
      font-weight: 500;
    }
    .form-group input, .form-group select, .form-group textarea {
      padding-block: 0.6rem;
      padding-inline: 0.85rem;
      background: #081A35;
      border: 1px solid #1E3A5F;
      border-radius: 0.375rem;
      color: #F8FAFC;
      font-size: 0.9rem;
    }
    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }
    .modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
      margin-block-start: 1rem;
    }
    .credential-card {
      background: #061329;
      border: 1px solid #059669;
      border-radius: 0.5rem;
      padding-block: 1rem;
      padding-inline: 1rem;
      margin-block: 1rem;
    }
    .credential-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-block-end: 0.5rem;
      font-size: 0.85rem;
      color: #10B981;
      font-weight: 600;
    }
    .btn-copy {
      padding-block: 0.25rem;
      padding-inline: 0.65rem;
      background: #059669;
      color: #FFFFFF;
      border: none;
      border-radius: 0.25rem;
      cursor: pointer;
      font-size: 0.8rem;
    }
    .credential-text {
      white-space: pre-wrap;
      font-family: monospace;
      color: #E2E8F0;
      font-size: 0.85rem;
      line-height: 1.4;
      margin: 0;
    }
  `]
})
export class AccountsComponent implements OnInit {
  readonly adminStore = inject(AdminStore);
  readonly impersonationStore = inject(AdminImpersonationStore);

  searchQuery = '';
  statusFilter = '';
  showCreateModal = false;
  createdCredentialsCard: string | null = null;

  targetWorkspaceForSuspension: AdminWorkspaceSummary | null = null;
  suspensionReason = '';

  newWorkspace: CreateWorkspaceRequest = {
    name: '',
    ownerName: '',
    ownerPhone: '',
    ownerEmail: '',
    type: 'Center',
    packageTier: 'pro',
    maxStudents: 500,
    agreedPrice: 1500
  };

  ngOnInit(): void {
    this.adminStore.loadWorkspaces().subscribe();
  }

  get filteredWorkspaces(): AdminWorkspaceSummary[] {
    return this.adminStore.workspaces().filter(ws => {
      const matchQuery = !this.searchQuery ||
        ws.name.includes(this.searchQuery) ||
        ws.code.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        ws.ownerPhone.includes(this.searchQuery);
      const matchStatus = !this.statusFilter || ws.status === this.statusFilter;
      return matchQuery && matchStatus;
    });
  }

  applyFilter(): void {
    // Reactive binding triggers filteredWorkspaces
  }

  impersonate(workspace: AdminWorkspaceSummary): void {
    this.impersonationStore.startImpersonation(workspace);
  }

  openCreateModal(): void {
    this.createdCredentialsCard = null;
    this.newWorkspace = {
      name: '',
      ownerName: '',
      ownerPhone: '',
      ownerEmail: '',
      type: 'Center',
      packageTier: 'pro',
      maxStudents: 500,
      agreedPrice: 1500
    };
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.createdCredentialsCard = null;
  }

  submitCreate(): void {
    this.adminStore.createWorkspace(this.newWorkspace).subscribe(res => {
      if (res) {
        this.createdCredentialsCard = `🎉 مرحباً بك في منصة تحضيرة التعليمية!
تم إنشاء حساب السنتر الخاص بك بنجاح:
🏫 اسم السنتر: ${res.name}
🔑 كود السنتر: ${res.code}
👤 المدير المسؤول: ${res.ownerName}
📱 رقم الدخول: ${res.ownerPhone}
🌐 رابط تسجيل الدخول: https://tahdeera.app/login`;
      }
    });
  }

  copyCredentials(): void {
    if (this.createdCredentialsCard && navigator.clipboard) {
      navigator.clipboard.writeText(this.createdCredentialsCard);
    }
  }

  openSuspendModal(ws: AdminWorkspaceSummary): void {
    this.targetWorkspaceForSuspension = ws;
    this.suspensionReason = '';
  }

  confirmSuspension(): void {
    if (!this.targetWorkspaceForSuspension || this.suspensionReason.trim().length < 5) return;
    const wsId = this.targetWorkspaceForSuspension.id;
    this.adminStore.suspendWorkspace(wsId, { suspensionReason: this.suspensionReason.trim() }).subscribe(() => {
      this.targetWorkspaceForSuspension = null;
    });
  }

  reactivate(ws: AdminWorkspaceSummary): void {
    this.adminStore.reactivateWorkspace(ws.id).subscribe();
  }
}
