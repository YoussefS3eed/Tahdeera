import { Component, EventEmitter, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthStore } from '../../../core/stores/auth.store';
import { RouterModule } from '@angular/router';
import { LocalizationService } from '../../../core/services/localization.service';
import { NavigationItem } from '../../../core/models/public.models';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <aside class="desktop-sidebar" aria-label="الشريط الجانبي">
      <div class="sidebar-top-section">
        <!-- Brand Logo Header -->
        <div class="sidebar-brand-header" routerLink="/">
          <div class="brand-cap-box">
            <svg class="brand-cap-svg" viewBox="0 0 48 48" fill="none" aria-hidden="true">
              <path d="M24 6L42 14L24 22L6 14L24 6Z" stroke="#60A5FA" stroke-width="2.5" stroke-linejoin="round"/>
              <path d="M36 17V27C36 31 24 34 24 34C24 34 12 31 12 27V17" stroke="#93C5FD" stroke-width="2.5" stroke-linecap="round"/>
              <path d="M10 34L24 40L38 34" stroke="#F59E0B" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M10 39L24 45L38 39" stroke="#F59E0B" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>
          <div class="brand-info">
            <span class="brand-title">{{ localization.translate('SHELL.APP_NAME') || 'تحضيرة' }}</span>
            <span class="brand-divider"></span>
            <span class="brand-desc">{{ localization.translate('SHELL.APP_TAGLINE') || 'حضورك الذكي' }}</span>
          </div>
        </div>

        <div class="workspace-role-banner">
          <span>مساحة السنتر — سنتر الإتقان</span>
        </div>

        <!-- 14 Navigation Tabs -->
        <nav class="sidebar-nav" aria-label="التنقل الرئيسي">
          @for (item of navItems; track item.id) {
            <a
              [routerLink]="item.route"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{ exact: item.route === '/workspace' }"
              class="nav-tab-item"
              (click)="tabSelected.emit(item.id)"
            >
              <span class="material-symbols-outlined nav-icon">{{ item.icon }}</span>
              <span class="nav-label">{{ localization.translate(item.labelKey) }}</span>
            </a>
          }
        </nav>
      </div>

      <!-- Support Card & Exit Footer -->
      <div class="sidebar-bottom-section">
        <div class="support-card">
          <div class="support-card-title">نحن هنا لمساعدتك</div>
          <p class="support-card-desc">تجديد الاشتراك وإعداد قارئ الحضور والدعم الفني</p>
          <a href="tel:01272785618" class="support-card-phone ltr-num font-mono">01272785618</a>
        </div>

        <a routerLink="/" class="btn-exit-portal">
          <span class="material-symbols-outlined exit-icon">logout</span>
          <span>العودة لبوابة الزوار</span>
        </a>
      </div>
    </aside>
  `,
  styles: [`
    :host {
      display: block;
      height: 100%;
    }

    .desktop-sidebar {
      width: 260px;
      height: 100%;
      background-color: var(--bg-sidebar, #081e3f);
      color: #ffffff;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      border-inline-end: 1px solid rgba(255, 255, 255, 0.08);
      user-select: none;
      box-sizing: border-box;
    }

    .sidebar-top-section {
      padding-block: 1.25rem;
      padding-inline: 1rem;
      overflow-y: auto;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .sidebar-brand-header {
      display: inline-flex;
      align-items: center;
      gap: 0.75rem;
      cursor: pointer;
      text-decoration: none;
      padding-inline: 0.25rem;
    }

    .brand-cap-box {
      width: 2.5rem;
      height: 2.5rem;
      border-radius: var(--radius-md, 12px);
      background-color: rgba(96, 165, 250, 0.15);
      border: 1px solid rgba(96, 165, 250, 0.3);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .brand-cap-svg {
      width: 1.75rem;
      height: 1.75rem;
    }

    .brand-info {
      display: flex;
      flex-direction: column;
    }

    .brand-title {
      font-size: 1.25rem;
      font-weight: 800;
      color: #ffffff;
      line-height: 1.2;
    }

    .brand-divider {
      width: 3rem;
      height: 3px;
      background-color: #f59e0b;
      border-radius: 9999px;
      margin-block: 2px;
    }

    .brand-desc {
      font-size: 0.6875rem;
      color: #cbd5e1;
    }

    .workspace-role-banner {
      font-size: 0.6875rem;
      font-weight: 600;
      color: #94a3b8;
      padding-inline: 0.5rem;
      padding-block-end: 0.25rem;
    }

    .sidebar-nav {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .nav-tab-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding-block: 0.625rem;
      padding-inline: 0.875rem;
      border-radius: var(--radius-md, 10px);
      font-size: 0.8125rem;
      font-weight: 600;
      color: #cbd5e1;
      text-decoration: none;
      transition: all 0.15s ease;
      border-inline-start: 4px solid transparent;
      text-align: start;
    }

    .nav-tab-item:hover {
      background-color: rgba(255, 255, 255, 0.08);
      color: #ffffff;
    }

    .nav-tab-item.active {
      background-color: rgba(37, 99, 235, 0.25);
      color: #ffffff;
      border-inline-start-color: var(--brand-accent, #3b82f6);
      font-weight: 700;
    }

    .nav-icon {
      font-size: 1.25rem;
      flex-shrink: 0;
    }

    .nav-label {
      flex: 1;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .sidebar-bottom-section {
      padding-block: 1rem;
      padding-inline: 1rem;
      border-block-start: 1px solid rgba(255, 255, 255, 0.08);
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .support-card {
      background-color: rgba(255, 255, 255, 0.05);
      border: 1px solid rgba(255, 255, 255, 0.08);
      border-radius: var(--radius-md, 10px);
      padding: 0.875rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
      text-align: start;
    }

    .support-card-title {
      font-size: 0.75rem;
      font-weight: 700;
      color: #ffffff;
    }

    .support-card-desc {
      font-size: 0.6875rem;
      color: #94a3b8;
      line-height: 1.4;
      margin: 0;
    }

    .support-card-phone {
      font-size: 0.8125rem;
      font-weight: 700;
      color: #f59e0b;
      text-decoration: none;
      padding-block-start: 0.25rem;
    }

    .support-card-phone:hover {
      text-decoration: underline;
    }

    .btn-exit-portal {
      background: transparent;
      border: none;
      font-family: inherit;
      width: 100%;
      text-align: start;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding-block: 0.5rem;
      padding-inline: 0.5rem;
      font-size: 0.75rem;
      color: #94a3b8;
      text-decoration: none;
      transition: color 0.15s ease;
      cursor: pointer;
    }

    .btn-exit-portal:hover {
      color: #ffffff;
    }

    .exit-icon {
      font-size: 1.125rem;
    }
  `]
})
export class SidebarComponent {
  readonly localization = inject(LocalizationService);
  private readonly authStore = inject(AuthStore, { optional: true });
  private readonly router = inject(Router, { optional: true });

  logout(): void {
    if (this.authStore) {
      this.authStore.logout();
    }
    if (this.router) {
      this.router.navigate(['/login']);
    }
  }

  @Output() tabSelected = new EventEmitter<string>();

  readonly navItems: NavigationItem[] = [
    { id: 'dashboard', labelKey: 'NAV.DASHBOARD', icon: 'grid_view', route: '/workspace/dashboard' },
    { id: 'nfc-station', labelKey: 'NAV.NFC_STATION', icon: 'contactless', route: '/workspace/nfc-station' },
    { id: 'attendance', labelKey: 'NAV.ATTENDANCE', icon: 'fact_check', route: '/workspace/attendance' },
    { id: 'students', labelKey: 'NAV.STUDENTS', icon: 'groups', route: '/workspace/students' },
    { id: 'cards', labelKey: 'NAV.CARDS', icon: 'credit_card', route: '/workspace/cards' },
    { id: 'groups', labelKey: 'NAV.GROUPS', icon: 'layers', route: '/workspace/groups' },
    { id: 'schedule', labelKey: 'NAV.SCHEDULE', icon: 'calendar_month', route: '/workspace/schedule' },
    { id: 'payments', labelKey: 'NAV.PAYMENTS', icon: 'account_balance_wallet', route: '/workspace/payments' },
    { id: 'whatsapp', labelKey: 'NAV.WHATSAPP', icon: 'chat', route: '/workspace/whatsapp' },
    { id: 'lms', labelKey: 'NAV.LMS', icon: 'quiz', route: '/workspace/lms' },
    { id: 'reports', labelKey: 'NAV.REPORTS', icon: 'bar_chart', route: '/workspace/reports' },
    { id: 'team', labelKey: 'NAV.TEAM', icon: 'verified_user', route: '/workspace/team' },
    { id: 'settings', labelKey: 'NAV.SETTINGS', icon: 'settings', route: '/workspace/settings' },
    { id: 'ai-assistant', labelKey: 'NAV.AI_ASSISTANT', icon: 'auto_awesome', route: '/workspace/ai-assistant' }
  ];
}
