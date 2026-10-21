import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthStore } from '../../stores/auth.store';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  template: `
    <div class="admin-shell">
      <!-- Top Platform Header -->
      <header class="admin-header">
        <div class="header-inner">
          <div class="brand-section">
            <span class="shield-badge" aria-hidden="true">🛡️</span>
            <div>
              <h1 class="platform-title">{{ 'ADMIN_SHELL.TITLE' | translate }}</h1>
              <span class="platform-subtitle font-mono">Tahdeera Super Admin Control</span>
            </div>
          </div>
          <div class="user-section">
            <span class="admin-name">{{ authStore.user()?.fullName || ('ADMIN_SHELL.SUPER_ADMIN_BADGE' | translate) }}</span>
            <button type="button" class="logout-btn" (click)="logout()">خروج</button>
          </div>
        </div>
      </header>

      <!-- Navigation Tabs Bar -->
      <nav class="admin-nav" aria-label="أقسام الإدارة المركزية">
        <div class="nav-inner">
          @for (tab of navTabs; track tab.path) {
            <a
              [routerLink]="['/admin', tab.path]"
              routerLinkActive="active"
              class="nav-tab"
            >
              <span class="tab-icon" aria-hidden="true">{{ tab.icon }}</span>
              <span class="tab-label">{{ tab.key | translate }}</span>
            </a>
          }
        </div>
      </nav>

      <!-- Sub-view content -->
      <main class="admin-viewport">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .admin-shell {
      min-block-size: 100vh;
      background: #061329;
      color: #F8FAFC;
      display: flex;
      flex-direction: column;
    }
    .admin-header {
      background: #081E3F;
      border-block-end: 1px solid #1E293B;
      padding-block: 0.85rem;
      padding-inline: 1.5rem;
    }
    .header-inner {
      max-inline-size: 1440px;
      margin-inline: auto;
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .brand-section {
      display: flex;
      align-items: center;
      gap: 0.85rem;
    }
    .shield-badge {
      font-size: 1.75rem;
    }
    .platform-title {
      font-size: 1.15rem;
      font-weight: 700;
      color: #F8FAFC;
      margin: 0;
    }
    .platform-subtitle {
      font-size: 0.75rem;
      color: #94A3B8;
    }
    .user-section {
      display: flex;
      align-items: center;
      gap: 1rem;
    }
    .admin-name {
      font-size: 0.9rem;
      color: #CBD5E1;
      font-weight: 500;
    }
    .logout-btn {
      padding-block: 0.35rem;
      padding-inline: 0.85rem;
      background: transparent;
      border: 1px solid #475569;
      color: #E2E8F0;
      border-radius: 0.375rem;
      font-size: 0.85rem;
      cursor: pointer;
      transition: background-color 0.2s, border-color 0.2s;
    }
    .logout-btn:hover {
      background: #1E293B;
      border-color: #94A3B8;
    }
    .admin-nav {
      background: #0C2340;
      border-block-end: 1px solid #1E293B;
      overflow-x: auto;
      -webkit-overflow-scrolling: touch;
    }
    .nav-inner {
      max-inline-size: 1440px;
      margin-inline: auto;
      display: flex;
      padding-inline: 1rem;
      gap: 0.5rem;
    }
    .nav-tab {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding-block: 0.85rem;
      padding-inline: 1rem;
      color: #94A3B8;
      text-decoration: none;
      font-size: 0.9rem;
      font-weight: 500;
      border-block-end: 2px solid transparent;
      white-space: nowrap;
      transition: color 0.2s, border-color 0.2s;
    }
    .nav-tab:hover {
      color: #F8FAFC;
    }
    .nav-tab.active {
      color: #38BDF8;
      border-block-end-color: #38BDF8;
      font-weight: 600;
    }
    .admin-viewport {
      flex: 1;
      max-inline-size: 1440px;
      inline-size: 100%;
      margin-inline: auto;
      padding-block: 1.5rem;
      padding-inline: 1.5rem;
    }
  `]
})
export class AdminLayoutComponent {
  readonly authStore = inject(AuthStore);
  private router = inject(Router);

  readonly navTabs = [
    { path: 'overview', key: 'ADMIN_SHELL.TAB_OVERVIEW', icon: '📊' },
    { path: 'accounts', key: 'ADMIN_SHELL.TAB_ACCOUNTS', icon: '🏫' },
    { path: 'subscriptions', key: 'ADMIN_SHELL.TAB_SUBSCRIPTIONS', icon: '🔄' },
    { path: 'packages', key: 'ADMIN_SHELL.TAB_PACKAGES', icon: '📦' },
    { path: 'revenue', key: 'ADMIN_SHELL.TAB_REVENUE', icon: '💰' },
    { path: 'whatsapp', key: 'ADMIN_SHELL.TAB_WHATSAPP', icon: '💬' },
    { path: 'settings', key: 'ADMIN_SHELL.TAB_SETTINGS', icon: '⚙️' },
    { path: 'audit-logs', key: 'ADMIN_SHELL.TAB_AUDIT', icon: '📋' }
  ];

  logout(): void {
    this.authStore.logout();
    this.router.navigate(['/login']);
  }
}
