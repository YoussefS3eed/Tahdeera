import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AdminStore } from '../../../core/stores/admin.store';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

@Component({
  selector: 'app-admin-overview',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  template: `
    <div class="overview-container">
      <header class="page-header">
        <h2 class="page-title">{{ 'ADMIN_OVERVIEW.PAGE_TITLE' | translate }}</h2>
        <p class="page-subtitle">{{ 'ADMIN_OVERVIEW.SUBTITLE' | translate }}</p>
      </header>

      <!-- 4 Aggregate KPI Cards -->
      @if (adminStore.overviewKpis(); as kpis) {
        <div class="kpi-grid">
          <div class="kpi-card">
            <div class="kpi-header">
              <span class="kpi-title">{{ 'ADMIN_OVERVIEW.KPI_REGISTERED' | translate }}</span>
              <span class="kpi-icon" aria-hidden="true">🏫</span>
            </div>
            <div class="kpi-value ltr-num font-mono">{{ kpis.activeCentersCount }}</div>
            <div class="kpi-meta">
              <span>{{ 'ADMIN_OVERVIEW.STATUS_ACTIVE' | translate }}: <strong class="ltr-num font-mono">{{ kpis.activeCentersCount }}</strong></span>
              <span>{{ 'ADMIN_OVERVIEW.STATUS_SUSPENDED' | translate }}: <strong class="ltr-num font-mono">{{ kpis.suspendedCentersCount }}</strong></span>
            </div>
          </div>

          <div class="kpi-card">
            <div class="kpi-header">
              <span class="kpi-title">{{ 'ADMIN_OVERVIEW.KPI_STUDENTS_USAGE' | translate }}</span>
              <span class="kpi-icon" aria-hidden="true">🎓</span>
            </div>
            <div class="kpi-value ltr-num font-mono">{{ kpis.activeStudentsCount }}</div>
            <div class="kpi-meta text-muted">{{ 'ADMIN_OVERVIEW.KPI_ANONYMOUS_COUNTER' | translate }}</div>
          </div>

          <div class="kpi-card highlight">
            <div class="kpi-header">
              <span class="kpi-title">{{ 'ADMIN_OVERVIEW.KPI_COLLECTED_REVENUE' | translate }}</span>
              <span class="kpi-icon" aria-hidden="true">💰</span>
            </div>
            <div class="kpi-value ltr-num font-mono">{{ kpis.monthlyRevenueEgp }} <span class="currency">ج.م</span></div>
            <div class="kpi-meta text-emerald">متحصلات اشتراكات B2B</div>
          </div>

          <div class="kpi-card alert">
            <div class="kpi-header">
              <span class="kpi-title">{{ 'ADMIN_OVERVIEW.KPI_EXPIRING_SOON' | translate }}</span>
              <span class="kpi-icon" aria-hidden="true">⏳</span>
            </div>
            <div class="kpi-value ltr-num font-mono">{{ kpis.expiringIn7DaysCount }}</div>
            <div class="kpi-meta text-amber">خلال 7 أيام القادمة</div>
          </div>
        </div>
      }

      <div class="content-grid">
        <!-- 7-Day Renewal Watchlist -->
        <section class="section-card watchlist-card">
          <div class="section-header">
            <h3 class="section-title">{{ 'ADMIN_OVERVIEW.WATCHLIST_TITLE' | translate }}</h3>
            <a routerLink="/admin/subscriptions" class="view-all-link">{{ 'ADMIN_OVERVIEW.VIEW_ALL_SUBS' | translate }} &larr;</a>
          </div>
          <div class="table-responsive">
            <table class="data-table">
              <thead>
                <tr>
                  <th>{{ 'ADMIN_OVERVIEW.COL_CENTER' | translate }}</th>
                  <th>{{ 'ADMIN_OVERVIEW.COL_EXPIRY' | translate }}</th>
                  <th>{{ 'ADMIN_OVERVIEW.COL_REMAINING' | translate }}</th>
                  <th>السعر المتفق عليه</th>
                  <th>{{ 'ADMIN_OVERVIEW.COL_ACTION' | translate }}</th>
                </tr>
              </thead>
              <tbody>
                @for (item of adminStore.renewalWatchlist(); track item.workspaceId) {
                  <tr>
                    <td>
                      <div class="workspace-cell">
                        <strong>{{ item.workspaceName }}</strong>
                        <span class="owner-hint">{{ item.ownerName }}</span>
                      </div>
                    </td>
                    <td class="ltr-num font-mono">{{ item.expiresAt | date:'yyyy/MM/dd' }}</td>
                    <td>
                      <span class="badge" [class.badge-warning]="item.daysRemaining <= 7" [class.badge-danger]="item.daysRemaining <= 2">
                        <span class="ltr-num font-mono">{{ item.daysRemaining }}</span> أيام
                      </span>
                    </td>
                    <td class="ltr-num font-mono">{{ item.agreedPrice }} ج.م</td>
                    <td>
                      <a [routerLink]="['/admin/subscriptions']" [queryParams]="{ workspaceId: item.workspaceId }" class="btn-table">
                        {{ 'ADMIN_OVERVIEW.ACTION_RENEW' | translate }}
                      </a>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="5" class="empty-state">لا توجد اشتراكات توشك على الانتهاء حالياً.</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </section>

        <!-- Account Status Breakdown & Recent Audit -->
        <div class="side-column">
          <section class="section-card">
            <h3 class="section-title">{{ 'ADMIN_OVERVIEW.ACCOUNT_STATUS_TITLE' | translate }}</h3>
            <div class="status-bars">
              <div class="status-bar-row">
                <span class="status-label">{{ 'ADMIN_OVERVIEW.STATUS_ACTIVE' | translate }}</span>
                <span class="status-count ltr-num font-mono">{{ adminStore.overviewKpis()?.activeCentersCount || 0 }}</span>
              </div>
              <div class="progress-bar">
                <div class="progress-fill active" style="inline-size: 85%;"></div>
              </div>

              <div class="status-bar-row">
                <span class="status-label">{{ 'ADMIN_OVERVIEW.STATUS_SUSPENDED' | translate }}</span>
                <span class="status-count ltr-num font-mono">{{ adminStore.overviewKpis()?.suspendedCentersCount || 0 }}</span>
              </div>
              <div class="progress-bar">
                <div class="progress-fill suspended" style="inline-size: 15%;"></div>
              </div>
            </div>
          </section>

          <section class="section-card">
            <div class="section-header">
              <h3 class="section-title">{{ 'ADMIN_OVERVIEW.RECENT_AUDIT_TITLE' | translate }}</h3>
              <a routerLink="/admin/audit-logs" class="view-all-link">{{ 'ADMIN_OVERVIEW.VIEW_ALL_AUDIT' | translate }} &larr;</a>
            </div>
            <ul class="audit-mini-feed">
              @for (log of adminStore.auditLogs().slice(0, 4); track log.id) {
                <li class="audit-mini-item">
                  <div class="audit-mini-dot"></div>
                  <div class="audit-mini-body">
                    <span class="audit-mini-action">{{ log.action }}</span>
                    <span class="audit-mini-target">{{ log.targetName }}</span>
                    <span class="audit-mini-time ltr-num font-mono">{{ log.createdAt | date:'HH:mm - yyyy/MM/dd' }}</span>
                  </div>
                </li>
              }
            </ul>
          </section>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .overview-container {
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
    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
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
      gap: 0.5rem;
    }
    .kpi-card.highlight {
      border-color: #059669;
    }
    .kpi-card.alert {
      border-color: #D97706;
    }
    .kpi-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      color: #94A3B8;
      font-size: 0.9rem;
      font-weight: 500;
    }
    .kpi-icon {
      font-size: 1.25rem;
    }
    .kpi-value {
      font-size: 1.85rem;
      font-weight: 700;
      color: #F8FAFC;
    }
    .currency {
      font-size: 1rem;
      font-weight: 500;
      color: #94A3B8;
    }
    .kpi-meta {
      font-size: 0.85rem;
      display: flex;
      gap: 1rem;
      color: #94A3B8;
    }
    .text-emerald { color: #10B981; }
    .text-amber { color: #F59E0B; }
    .content-grid {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 1.5rem;
    }
    @media (max-width: 960px) {
      .content-grid {
        grid-template-columns: 1fr;
      }
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
    .section-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .section-title {
      font-size: 1.1rem;
      font-weight: 600;
      color: #F8FAFC;
      margin: 0;
    }
    .view-all-link {
      font-size: 0.85rem;
      color: #38BDF8;
      text-decoration: none;
    }
    .view-all-link:hover {
      text-decoration: underline;
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
      padding-block: 0.65rem;
      padding-inline: 0.75rem;
      background: #081A35;
      color: #94A3B8;
      font-weight: 500;
      border-block-end: 1px solid #1E3A5F;
    }
    .data-table td {
      padding-block: 0.75rem;
      padding-inline: 0.75rem;
      border-block-end: 1px solid #132D50;
      color: #CBD5E1;
    }
    .workspace-cell {
      display: flex;
      flex-direction: column;
    }
    .owner-hint {
      font-size: 0.75rem;
      color: #94A3B8;
    }
    .badge {
      display: inline-block;
      padding-block: 0.2rem;
      padding-inline: 0.6rem;
      border-radius: 9999px;
      font-size: 0.8rem;
      font-weight: 600;
    }
    .badge-warning {
      background: rgba(245, 158, 11, 0.2);
      color: #FBBF24;
    }
    .badge-danger {
      background: rgba(239, 68, 68, 0.2);
      color: #F87171;
    }
    .btn-table {
      display: inline-block;
      padding-block: 0.3rem;
      padding-inline: 0.65rem;
      background: #0284C7;
      color: #FFFFFF;
      border-radius: 0.375rem;
      font-size: 0.8rem;
      text-decoration: none;
      font-weight: 600;
    }
    .empty-state {
      text-align: center;
      padding-block: 2rem;
      color: #94A3B8;
    }
    .side-column {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }
    .status-bars {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }
    .status-bar-row {
      display: flex;
      justify-content: space-between;
      font-size: 0.85rem;
      color: #CBD5E1;
    }
    .progress-bar {
      inline-size: 100%;
      block-size: 0.5rem;
      background: #1E3A5F;
      border-radius: 9999px;
      overflow: hidden;
    }
    .progress-fill.active {
      background: #10B981;
      block-size: 100%;
    }
    .progress-fill.suspended {
      background: #EF4444;
      block-size: 100%;
    }
    .audit-mini-feed {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: 0.85rem;
    }
    .audit-mini-item {
      display: flex;
      gap: 0.65rem;
    }
    .audit-mini-dot {
      inline-size: 0.5rem;
      block-size: 0.5rem;
      border-radius: 9999px;
      background: #38BDF8;
      margin-block-start: 0.4rem;
    }
    .audit-mini-body {
      display: flex;
      flex-direction: column;
      font-size: 0.85rem;
    }
    .audit-mini-action {
      font-weight: 600;
      color: #F8FAFC;
    }
    .audit-mini-target {
      color: #94A3B8;
    }
    .audit-mini-time {
      font-size: 0.75rem;
      color: #64748B;
    }
  `]
})
export class AdminOverviewComponent implements OnInit {
  readonly adminStore = inject(AdminStore);

  ngOnInit(): void {
    this.adminStore.loadOverview().subscribe();
    this.adminStore.loadAuditLogs().subscribe();
  }
}
