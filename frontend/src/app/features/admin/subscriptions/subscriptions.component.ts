import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AdminStore } from '../../../core/stores/admin.store';
import { AdminWorkspaceSummary, SubscriptionPeriodPreset } from '../../../core/models/admin.models';
import { RenewSubscriptionRequest } from '../../../core/models/admin.requests';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

@Component({
  selector: 'app-subscriptions',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  template: `
    <div class="subscriptions-container">
      <header class="page-header">
        <h2 class="page-title">{{ 'ADMIN_SUBSCRIPTIONS.PAGE_TITLE' | translate }}</h2>
        <p class="page-subtitle">{{ 'ADMIN_SUBSCRIPTIONS.SUBTITLE' | translate }}</p>
      </header>

      <div class="section-card">
        <div class="table-responsive">
          <table class="data-table">
            <thead>
              <tr>
                <th>{{ 'ADMIN_OVERVIEW.COL_CENTER' | translate }}</th>
                <th>الباقة الحالية</th>
                <th>{{ 'ADMIN_SUBSCRIPTIONS.EXPIRATION' | translate }}</th>
                <th>{{ 'ADMIN_SUBSCRIPTIONS.DAYS_LEFT' | translate }}</th>
                <th>{{ 'ADMIN_SUBSCRIPTIONS.PAYMENT_STATUS' | translate }}</th>
                <th>{{ 'ADMIN_SUBSCRIPTIONS.PRICE' | translate }}</th>
                <th>{{ 'ADMIN_OVERVIEW.COL_ACTION' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (ws of adminStore.workspaces(); track ws.id) {
                <tr>
                  <td>
                    <strong>{{ ws.name }}</strong>
                    <div class="code-hint ltr-num font-mono">{{ ws.code }}</div>
                  </td>
                  <td><span class="tier-pill font-mono">{{ ws.packageTier }}</span></td>
                  <td class="ltr-num font-mono">{{ ws.subscriptionExpiresAt | date:'yyyy/MM/dd' }}</td>
                  <td>
                    <span class="badge" [class.badge-warning]="(ws.subscriptionDaysRemaining ?? 0) <= 7" [class.badge-success]="(ws.subscriptionDaysRemaining ?? 0) > 7">
                      <span class="ltr-num font-mono">{{ ws.subscriptionDaysRemaining ?? 0 }}</span> يوم
                    </span>
                  </td>
                  <td>
                    @if ((ws.subscriptionDaysRemaining ?? 0) > 7) {
                      <span class="badge badge-success">{{ 'ADMIN_OVERVIEW.STATUS_ACTIVE' | translate }}</span>
                    } @else if ((ws.subscriptionDaysRemaining ?? 0) > 0) {
                      <span class="badge badge-warning">{{ 'ADMIN_OVERVIEW.STATUS_EXPIRING' | translate }}</span>
                    } @else {
                      <span class="badge badge-danger">{{ 'ADMIN_OVERVIEW.STATUS_EXPIRED' | translate }}</span>
                    }
                  </td>
                  <td class="ltr-num font-mono">{{ ws.agreedPrice }} ج.م</td>
                  <td>
                    <button type="button" class="btn btn-sm btn-primary" (click)="openRenewModal(ws)">
                      {{ 'ADMIN_SUBSCRIPTIONS.RENEW_TITLE' | translate }}
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>

      <!-- Renew Modal -->
      @if (selectedWorkspace) {
        <div class="modal-backdrop">
          <div class="modal-card">
            <h3 class="modal-title">{{ 'ADMIN_SUBSCRIPTIONS.RENEW_MODAL_TITLE' | translate }}: {{ selectedWorkspace.name }}</h3>
            <div class="duration-presets">
              <span class="preset-label">{{ 'ADMIN_SUBSCRIPTIONS.PERIOD_LABEL' | translate }}</span>
              <div class="chips-row">
                <button
                  type="button"
                  class="preset-chip"
                  [class.active]="selectedPreset === '30D'"
                  (click)="applyPreset('30D', 30)"
                >
                  {{ 'ADMIN_SUBSCRIPTIONS.PERIOD_30D' | translate }}
                </button>
                <button
                  type="button"
                  class="preset-chip"
                  [class.active]="selectedPreset === '90D'"
                  (click)="applyPreset('90D', 90)"
                >
                  {{ 'ADMIN_SUBSCRIPTIONS.PERIOD_90D' | translate }}
                </button>
                <button
                  type="button"
                  class="preset-chip"
                  [class.active]="selectedPreset === '365D'"
                  (click)="applyPreset('365D', 365)"
                >
                  {{ 'ADMIN_SUBSCRIPTIONS.PERIOD_365D' | translate }}
                </button>
                <button
                  type="button"
                  class="preset-chip"
                  [class.active]="selectedPreset === '7D'"
                  (click)="applyPreset('7D', 7)"
                >
                  {{ 'ADMIN_SUBSCRIPTIONS.PERIOD_7D' | translate }}
                </button>
              </div>
            </div>

            <div class="form-row">
              <div class="form-group">
                <label>عدد الأيام المضافة *</label>
                <input
                  type="number"
                  [(ngModel)]="renewRequest.durationDays"
                  name="durationDays"
                  required
                  min="1"
                  class="ltr-num font-mono"
                  (ngModelChange)="calculateExpiryPreview()"
                />
              </div>
              <div class="form-group">
                <label>{{ 'ADMIN_SUBSCRIPTIONS.AMOUNT_LABEL' | translate }}</label>
                <input
                  type="number"
                  [(ngModel)]="renewRequest.agreedPrice"
                  name="agreedPrice"
                  required
                  min="0"
                  class="ltr-num font-mono"
                />
              </div>
            </div>

            <div class="form-group">
              <label>{{ 'ADMIN_SUBSCRIPTIONS.NOTES_LABEL' | translate }}</label>
              <input
                type="text"
                [(ngModel)]="renewRequest.internalNotes"
                name="internalNotes"
                placeholder="مثال: تجديد سنوي مع خصم 10% بموافقة الإدارة"
              />
            </div>

            <div class="expiry-preview">
              <span class="preview-label">تاريخ نهاية الاشتراك الجديد:</span>
              <strong class="preview-date ltr-num font-mono">{{ calculatedExpiryDate | date:'yyyy/MM/dd' }}</strong>
            </div>

            <div class="modal-actions">
              <button type="button" class="btn btn-primary" (click)="confirmRenew()">
                {{ 'ADMIN_SUBSCRIPTIONS.CONFIRM_RENEW_BTN' | translate }}
              </button>
              <button type="button" class="btn btn-secondary" (click)="selectedWorkspace = null">{{ 'common.cancel' | translate }}</button>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .subscriptions-container {
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
    .code-hint {
      font-size: 0.75rem;
      color: #38BDF8;
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
    .badge-success { background: rgba(16, 185, 129, 0.2); color: #34D399; }
    .badge-warning { background: rgba(245, 158, 11, 0.2); color: #FBBF24; }
    .badge-danger { background: rgba(239, 68, 68, 0.2); color: #F87171; }
    .btn {
      padding-block: 0.5rem;
      padding-inline: 1rem;
      border-radius: 0.375rem;
      font-weight: 600;
      font-size: 0.85rem;
      cursor: pointer;
      border: none;
    }
    .btn-sm { padding-block: 0.35rem; padding-inline: 0.75rem; }
    .btn-primary { background: #0284C7; color: #FFFFFF; }
    .btn-primary:hover { background: #0369A1; }
    .btn-secondary { background: #334155; color: #F8FAFC; }
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
    .modal-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: #F8FAFC;
      margin: 0;
    }
    .duration-presets {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }
    .preset-label {
      font-size: 0.85rem;
      color: #94A3B8;
    }
    .chips-row {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }
    .preset-chip {
      padding-block: 0.4rem;
      padding-inline: 0.75rem;
      background: #081A35;
      border: 1px solid #1E3A5F;
      color: #CBD5E1;
      border-radius: 9999px;
      font-size: 0.8rem;
      cursor: pointer;
    }
    .preset-chip.active {
      background: #0284C7;
      border-color: #38BDF8;
      color: #FFFFFF;
      font-weight: 600;
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
    .expiry-preview {
      background: #061329;
      border: 1px solid #1E3A5F;
      padding-block: 0.75rem;
      padding-inline: 1rem;
      border-radius: 0.5rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.9rem;
    }
    .preview-label { color: #94A3B8; }
    .preview-date { color: #38BDF8; font-size: 1.1rem; }
    .modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
      margin-block-start: 0.5rem;
    }
  `]
})
export class SubscriptionsComponent implements OnInit {
  readonly adminStore = inject(AdminStore);
  private route = inject(ActivatedRoute);

  selectedWorkspace: AdminWorkspaceSummary | null = null;
  selectedPreset: SubscriptionPeriodPreset = '30D';
  calculatedExpiryDate = new Date();

  renewRequest: RenewSubscriptionRequest = {
    durationDays: 30,
    agreedPrice: 1500,
    internalNotes: ''
  };

  ngOnInit(): void {
    this.adminStore.loadWorkspaces().subscribe(workspaces => {
      this.route.queryParams.subscribe(params => {
        if (params['workspaceId']) {
          const found = this.adminStore.workspaces().find(w => w.id === params['workspaceId']);
          if (found) {
            this.openRenewModal(found);
          }
        }
      });
    });
  }

  openRenewModal(ws: AdminWorkspaceSummary): void {
    this.selectedWorkspace = ws;
    this.applyPreset('30D', 30);
    this.renewRequest.agreedPrice = ws.agreedPrice ?? ws.contractedPrice ?? 0;
    this.calculateExpiryPreview();
  }

  applyPreset(preset: SubscriptionPeriodPreset, days: number): void {
    this.selectedPreset = preset;
    this.renewRequest.durationDays = days;
    this.calculateExpiryPreview();
  }

  calculateExpiryPreview(): void {
    if (!this.selectedWorkspace) return;
    const base = new Date(this.selectedWorkspace.subscriptionExpiresAt);
    const start = base.getTime() > Date.now() ? base : new Date();
    this.calculatedExpiryDate = new Date(start.getTime() + (this.renewRequest.durationDays || 0) * 86400000);
  }

  confirmRenew(): void {
    if (!this.selectedWorkspace) return;
    this.adminStore.renewSubscription(this.selectedWorkspace.id, this.renewRequest).subscribe(() => {
      this.selectedWorkspace = null;
    });
  }
}
