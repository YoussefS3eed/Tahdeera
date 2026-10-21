import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminStore } from '../../../core/stores/admin.store';
import { PackageTemplate, AdminWorkspaceSummary, GranularFeatureFlags } from '../../../core/models/admin.models';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

@Component({
  selector: 'app-packages',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  template: `
    <div class="packages-container">
      <header class="page-header">
        <h2 class="page-title">{{ 'ADMIN_PACKAGES.PAGE_TITLE' | translate }}</h2>
        <p class="page-subtitle">{{ 'ADMIN_PACKAGES.SUBTITLE' | translate }}</p>
      </header>

      <!-- 3 Tier Cards -->
      <div class="tiers-grid">
        @for (pkg of adminStore.packages(); track pkg.id) {
          <div class="tier-card" [class.highlight]="pkg.tier === 'pro'">
            <div class="tier-header">
              <h3 class="tier-title">{{ pkg.name }}</h3>
              <span class="tier-price ltr-num font-mono">{{ pkg.priceEgp }} <span class="currency">ج.م / شهرياً</span></span>
              <p class="tier-desc">{{ pkg.description }}</p>
            </div>

            <div class="tier-quotas">
              <div class="quota-item">
                <span class="quota-label">الحد الأقصى للطلاب:</span>
                <strong class="ltr-num font-mono">{{ pkg.features.maxStudents }}</strong>
              </div>
              <div class="quota-item">
                <span class="quota-label">الحد الأقصى للمساعدين:</span>
                <strong class="ltr-num font-mono">{{ pkg.features.maxStaff }}</strong>
              </div>
            </div>

            <div class="features-list">
              <span class="features-header">الميزات المفعلة في الباقة:</span>
              <div class="feature-item" [class.enabled]="pkg.features.studentsAndGroups">
                <span class="status-icon">{{ pkg.features.studentsAndGroups ? '✓' : '✕' }}</span>
                <span>{{ 'ADMIN_PACKAGES.FLAG_STUDENTS' | translate }}</span>
              </div>
              <div class="feature-item" [class.enabled]="pkg.features.nfcAttendance">
                <span class="status-icon">{{ pkg.features.nfcAttendance ? '✓' : '✕' }}</span>
                <span>{{ 'ADMIN_PACKAGES.FLAG_NFC' | translate }}</span>
              </div>
              <div class="feature-item" [class.enabled]="pkg.features.paymentsAndFees">
                <span class="status-icon">{{ pkg.features.paymentsAndFees ? '✓' : '✕' }}</span>
                <span>{{ 'ADMIN_PACKAGES.FLAG_PAYMENTS' | translate }}</span>
              </div>
              <div class="feature-item" [class.enabled]="pkg.features.whatsappNotifications">
                <span class="status-icon">{{ pkg.features.whatsappNotifications ? '✓' : '✕' }}</span>
                <span>{{ 'ADMIN_PACKAGES.FLAG_WHATSAPP' | translate }}</span>
              </div>
              <div class="feature-item" [class.enabled]="pkg.features.examsAndQuestionBank">
                <span class="status-icon">{{ pkg.features.examsAndQuestionBank ? '✓' : '✕' }}</span>
                <span>{{ 'ADMIN_PACKAGES.FLAG_EXAMS' | translate }}</span>
              </div>
              <div class="feature-item" [class.enabled]="pkg.features.homeworkAndLibrary">
                <span class="status-icon">{{ pkg.features.homeworkAndLibrary ? '✓' : '✕' }}</span>
                <span>{{ 'ADMIN_PACKAGES.FLAG_HOMEWORK' | translate }}</span>
              </div>
              <div class="feature-item" [class.enabled]="pkg.features.aiAssistant">
                <span class="status-icon">{{ pkg.features.aiAssistant ? '✓' : '✕' }}</span>
                <span>{{ 'ADMIN_PACKAGES.FLAG_AI' | translate }}</span>
              </div>
              <div class="feature-item" [class.enabled]="pkg.features.excelExport">
                <span class="status-icon">{{ pkg.features.excelExport ? '✓' : '✕' }}</span>
                <span>{{ 'ADMIN_PACKAGES.FLAG_EXPORT' | translate }}</span>
              </div>
            </div>
          </div>
        }
      </div>

      <!-- Workspace Overrides Section -->
      <section class="section-card">
        <div class="section-header">
          <div>
            <h3 class="section-title">تخصيص ميزات استثنائية لسنتر محدد</h3>
            <p class="section-subtitle">إتاحة ميزات خارج حدود الباقة القياسية (تُطبق قاعدة Overrides Win)</p>
          </div>
        </div>

        <div class="table-responsive">
          <table class="data-table">
            <thead>
              <tr>
                <th>{{ 'ADMIN_OVERVIEW.COL_CENTER' | translate }}</th>
                <th>الباقة الحالية</th>
                <th>حالة التخصيص</th>
                <th>{{ 'ADMIN_OVERVIEW.COL_ACTION' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (ws of adminStore.workspaces(); track ws.id) {
                <tr>
                  <td><strong>{{ ws.name }}</strong></td>
                  <td><span class="tier-pill font-mono">{{ ws.packageTier }}</span></td>
                  <td>
                    @if (ws.featureOverrides) {
                      <span class="badge badge-warning">يوجد تخصيص استثنائي</span>
                    } @else {
                      <span class="badge badge-neutral">مطابق لإعدادات الباقة</span>
                    }
                  </td>
                  <td>
                    <button type="button" class="btn btn-sm btn-secondary" (click)="openOverridesModal(ws)">
                      تعديل التخصيصات
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </section>

      <!-- Custom Overrides Modal -->
      @if (targetWorkspace) {
        <div class="modal-backdrop">
          <div class="modal-card">
            <h3 class="modal-title">{{ 'ADMIN_PACKAGES.CUSTOM_MODAL_TITLE' | translate }}: {{ targetWorkspace.name }}</h3>
            <p class="modal-desc">
              تعديل الميزات هنا يمنح هذا السنتر صلاحيات خاصة تطغى على الباقة الأساسية.
            </p>

            <div class="flags-grid">
              <label class="toggle-row">
                <input type="checkbox" [(ngModel)]="activeOverrides.whatsappNotifications" />
                <span>{{ 'ADMIN_PACKAGES.FLAG_WHATSAPP' | translate }}</span>
              </label>
              <label class="toggle-row">
                <input type="checkbox" [(ngModel)]="activeOverrides.aiAssistant" />
                <span>{{ 'ADMIN_PACKAGES.FLAG_AI' | translate }}</span>
              </label>
              <label class="toggle-row">
                <input type="checkbox" [(ngModel)]="activeOverrides.excelExport" />
                <span>{{ 'ADMIN_PACKAGES.FLAG_EXPORT' | translate }}</span>
              </label>
              <label class="toggle-row">
                <input type="checkbox" [(ngModel)]="activeOverrides.nfcAttendance" />
                <span>{{ 'ADMIN_PACKAGES.FLAG_NFC' | translate }}</span>
              </label>
              <label class="toggle-row">
                <input type="checkbox" [(ngModel)]="activeOverrides.examsAndQuestionBank" />
                <span>{{ 'ADMIN_PACKAGES.FLAG_EXAMS' | translate }}</span>
              </label>
            </div>

            <div class="form-row">
              <div class="form-group">
                <label>الحد الأقصى للطلاب (استثنائي)</label>
                <input type="number" [(ngModel)]="activeOverrides.maxStudents" min="10" class="ltr-num font-mono" />
              </div>
              <div class="form-group">
                <label>الحد الأقصى للمساعدين (استثنائي)</label>
                <input type="number" [(ngModel)]="activeOverrides.maxStaff" min="1" class="ltr-num font-mono" />
              </div>
            </div>

            <div class="modal-actions-between">
              <button type="button" class="btn btn-outline-danger" (click)="resetOverrides()">
                استعادة الإعدادات الافتراضية للباقة
              </button>
              <div class="btn-group">
                <button type="button" class="btn btn-primary" (click)="saveOverrides()">
                  {{ 'ADMIN_PACKAGES.SAVE_FLAGS_BTN' | translate }}
                </button>
                <button type="button" class="btn btn-secondary" (click)="targetWorkspace = null">{{ 'common.cancel' | translate }}</button>
              </div>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .packages-container {
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
    .tiers-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1.5rem;
    }
    .tier-card {
      background: #0B2244;
      border: 1px solid #1E3A5F;
      border-radius: 0.75rem;
      padding-block: 1.5rem;
      padding-inline: 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }
    .tier-card.highlight {
      border-color: #38BDF8;
      box-shadow: 0 0 15px rgba(56, 189, 248, 0.1);
    }
    .tier-header {
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
    }
    .tier-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: #F8FAFC;
      margin: 0;
    }
    .tier-price {
      font-size: 1.5rem;
      font-weight: 700;
      color: #38BDF8;
    }
    .currency {
      font-size: 0.85rem;
      color: #94A3B8;
    }
    .tier-desc {
      font-size: 0.85rem;
      color: #CBD5E1;
      margin: 0;
    }
    .tier-quotas {
      background: #081A35;
      padding-block: 0.75rem;
      padding-inline: 1rem;
      border-radius: 0.5rem;
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
      font-size: 0.85rem;
    }
    .quota-item {
      display: flex;
      justify-content: space-between;
      color: #CBD5E1;
    }
    .features-list {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      font-size: 0.85rem;
    }
    .features-header {
      font-weight: 600;
      color: #94A3B8;
      margin-block-end: 0.25rem;
    }
    .feature-item {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      color: #64748B;
    }
    .feature-item.enabled {
      color: #F8FAFC;
    }
    .status-icon {
      font-weight: 700;
    }
    .feature-item.enabled .status-icon {
      color: #10B981;
    }
    .section-card {
      background: #0B2244;
      border: 1px solid #1E3A5F;
      border-radius: 0.75rem;
      padding-block: 1.25rem;
      padding-inline: 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }
    .section-title {
      font-size: 1.15rem;
      font-weight: 600;
      color: #F8FAFC;
      margin: 0;
    }
    .section-subtitle {
      font-size: 0.85rem;
      color: #94A3B8;
      margin: 0;
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
    .tier-pill {
      font-size: 0.8rem;
      color: #38BDF8;
      text-transform: uppercase;
    }
    .badge {
      display: inline-block;
      padding-block: 0.2rem;
      padding-inline: 0.6rem;
      border-radius: 9999px;
      font-size: 0.8rem;
      font-weight: 600;
    }
    .badge-warning { background: rgba(245, 158, 11, 0.2); color: #FBBF24; }
    .badge-neutral { background: #1E293B; color: #94A3B8; }
    .btn {
      padding-block: 0.5rem;
      padding-inline: 1rem;
      border-radius: 0.375rem;
      font-weight: 600;
      font-size: 0.85rem;
      cursor: pointer;
      border: 1px solid transparent;
    }
    .btn-sm { padding-block: 0.35rem; padding-inline: 0.75rem; }
    .btn-primary { background: #0284C7; color: #FFFFFF; }
    .btn-secondary { background: #334155; color: #F8FAFC; }
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
      max-inline-size: 36rem;
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
    .modal-desc {
      font-size: 0.9rem;
      color: #CBD5E1;
      margin: 0;
    }
    .flags-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0.75rem;
      background: #081A35;
      padding: 1rem;
      border-radius: 0.5rem;
    }
    .toggle-row {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      color: #F8FAFC;
      font-size: 0.85rem;
      cursor: pointer;
    }
    .form-group {
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
    }
    .form-group label {
      font-size: 0.85rem;
      color: #94A3B8;
    }
    .form-group input {
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
    .modal-actions-between {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-block-start: 1rem;
    }
    .btn-group {
      display: flex;
      gap: 0.5rem;
    }
  `]
})
export class PackagesComponent implements OnInit {
  readonly adminStore = inject(AdminStore);

  targetWorkspace: AdminWorkspaceSummary | null = null;
  activeOverrides: GranularFeatureFlags = {
    studentsAndGroups: true,
    nfcAttendance: false,
    paymentsAndFees: true,
    whatsappNotifications: false,
    examsAndQuestionBank: false,
    homeworkAndLibrary: false,
    aiAssistant: false,
    excelExport: false,
    maxStudents: 300,
    maxStaff: 3
  };

  ngOnInit(): void {
    this.adminStore.loadPackages().subscribe();
    this.adminStore.loadWorkspaces().subscribe();
  }

  openOverridesModal(ws: AdminWorkspaceSummary): void {
    this.targetWorkspace = ws;
    const currentPkg = this.adminStore.packages().find(p => p.tier === ws.packageTier);
    this.activeOverrides = {
      ...(currentPkg?.features || {
        studentsAndGroups: true,
        nfcAttendance: false,
        paymentsAndFees: true,
        whatsappNotifications: false,
        examsAndQuestionBank: false,
        homeworkAndLibrary: false,
        aiAssistant: false,
        excelExport: false,
        maxStudents: 300,
        maxStaff: 3
      }),
      ...(ws.featureOverrides || {})
    };
  }

  saveOverrides(): void {
    if (!this.targetWorkspace) return;
    this.adminStore.updatePackageOverrides(this.targetWorkspace.id, this.activeOverrides).subscribe(() => {
      this.targetWorkspace = null;
    });
  }

  resetOverrides(): void {
    if (!this.targetWorkspace) return;
    this.adminStore.resetPackageOverrides(this.targetWorkspace.id).subscribe(() => {
      this.targetWorkspace = null;
    });
  }
}
