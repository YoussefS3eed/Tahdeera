import { Component, EventEmitter, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LocalizationService } from '../../../core/services/localization.service';
import { NavigationItem } from '../../../core/models/public.models';

@Component({
  selector: 'app-mobile-nav',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <!-- Bottom Sticky Navigation Bar -->
    <nav class="mobile-bottom-nav" aria-label="شريط التنقل السفلي">
      @for (item of primaryItems; track item.id) {
        <a
          [routerLink]="item.route"
          routerLinkActive="active"
          class="nav-bottom-item"
        >
          <span class="material-symbols-outlined nav-item-icon">{{ item.icon }}</span>
          <span class="nav-item-label">{{ item.label }}</span>
        </a>
      }

      <!-- More Button -->
      <button
        type="button"
        class="nav-bottom-item more-btn"
        [class.active]="isDrawerOpen"
        (click)="toggleDrawer()"
        aria-label="المزيد من الخيارات"
      >
        <span class="material-symbols-outlined nav-item-icon">more_horiz</span>
        <span class="nav-item-label">{{ localization.translate('SHELL.MORE') || 'المزيد' }}</span>
      </button>
    </nav>

    <!-- Slide-over Bottom Sheet Drawer -->
    @if (isDrawerOpen) {
      <div class="drawer-backdrop" (click)="closeDrawer()">
        <div
          class="drawer-container"
          role="dialog"
          aria-modal="true"
          aria-labelledby="drawer-title"
          (click)="$event.stopPropagation()"
        >
          <div class="drawer-header">
            <div class="drawer-drag-handle"></div>
            <div class="drawer-header-content">
              <h3 id="drawer-title" class="drawer-title">
                {{ localization.translate('SHELL.MENU') || 'القائمة الكاملة' }}
              </h3>
              <button
                type="button"
                class="btn-close-drawer"
                (click)="closeDrawer()"
                aria-label="إغلاق القائمة"
              >
                <span class="material-symbols-outlined">close</span>
              </button>
            </div>
          </div>

          <div class="drawer-nav-grid">
            @for (item of secondaryItems; track item.id) {
              <a
                [routerLink]="item.route"
                routerLinkActive="active"
                class="drawer-nav-item"
                (click)="closeDrawer()"
              >
                <span class="material-symbols-outlined drawer-item-icon">{{ item.icon }}</span>
                <span class="drawer-item-label">{{ item.label }}</span>
              </a>
            }
          </div>

          <div class="drawer-footer">
            <span class="drawer-support-label">الدعم والتجديد:</span>
            <a href="tel:01272785618" class="drawer-phone ltr-num font-mono">01272785618</a>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    :host {
      display: block;
    }

    .mobile-bottom-nav {
      position: fixed;
      inset-inline-start: 0;
      inset-inline-end: 0;
      bottom: 0;
      height: calc(64px + env(safe-area-inset-bottom, 0px));
      padding-block-end: env(safe-area-inset-bottom, 0px);
      background-color: var(--bg-surface, #ffffff);
      border-block-start: 1px solid var(--border-subtle, #e2e8f0);
      display: flex;
      align-items: center;
      justify-content: space-around;
      z-index: 40;
      box-shadow: 0 -4px 6px -1px rgba(0, 0, 0, 0.05);
    }

    @media (min-width: 769px) {
      .mobile-bottom-nav {
        display: none;
      }
    }

    .nav-bottom-item {
      flex: 1;
      height: 100%;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 0.125rem;
      color: var(--text-secondary, #64748b);
      text-decoration: none;
      background: transparent;
      border: none;
      cursor: pointer;
      transition: color 0.15s ease;
      padding: 0;
    }

    .nav-bottom-item:hover {
      color: var(--brand-accent, #2563eb);
    }

    .nav-bottom-item.active {
      color: var(--brand-accent, #2563eb);
      font-weight: 700;
    }

    .nav-item-icon {
      font-size: 1.375rem;
    }

    .nav-item-label {
      font-size: 0.6875rem;
    }

    /* Slide-over Drawer */
    .drawer-backdrop {
      position: fixed;
      inset: 0;
      z-index: 50;
      background-color: rgba(15, 23, 42, 0.6);
      backdrop-filter: blur(2px);
      display: flex;
      align-items: flex-end;
      animation: fade-in 0.15s ease-out;
    }

    .drawer-container {
      width: 100%;
      max-height: 80vh;
      background-color: var(--bg-surface, #ffffff);
      border-start-start-radius: var(--radius-lg, 20px);
      border-start-end-radius: var(--radius-lg, 20px);
      border-block-start: 1px solid var(--border-subtle, #e2e8f0);
      display: flex;
      flex-direction: column;
      overflow-y: auto;
      animation: slide-up 0.25s cubic-bezier(0.16, 1, 0.3, 1);
      box-shadow: 0 -10px 25px -5px rgba(0, 0, 0, 0.15);
      padding-block-end: calc(1rem + env(safe-area-inset-bottom, 0px));
    }

    .drawer-header {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding-block: 0.75rem 0.5rem;
      padding-inline: 1.25rem;
      border-block-end: 1px solid var(--border-subtle, #e2e8f0);
    }

    .drawer-drag-handle {
      width: 2.5rem;
      height: 4px;
      background-color: var(--border-strong, #cbd5e1);
      border-radius: 9999px;
      margin-block-end: 0.75rem;
    }

    .drawer-header-content {
      width: 100%;
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .drawer-title {
      font-size: 1rem;
      font-weight: 700;
      color: var(--text-primary, #0f172a);
      margin: 0;
    }

    .btn-close-drawer {
      background: transparent;
      border: none;
      color: var(--text-secondary, #64748b);
      cursor: pointer;
      padding: 4px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 6px;
    }

    .drawer-nav-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 0.5rem;
      padding-block: 1rem;
      padding-inline: 1rem;
    }

    .drawer-nav-item {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      padding-block: 0.75rem;
      padding-inline: 0.875rem;
      background-color: var(--bg-surface-elevated, #f8fafc);
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: var(--radius-md, 10px);
      text-decoration: none;
      color: var(--text-primary, #0f172a);
      font-size: 0.8125rem;
      font-weight: 600;
      text-align: start;
    }

    .drawer-nav-item:hover,
    .drawer-nav-item.active {
      background-color: rgba(37, 99, 235, 0.1);
      border-color: var(--brand-accent, #3b82f6);
      color: var(--brand-accent, #2563eb);
    }

    .drawer-item-icon {
      font-size: 1.25rem;
      color: var(--brand-accent, #2563eb);
    }

    .drawer-footer {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding-block-start: 0.75rem;
      padding-inline: 1.25rem;
      border-block-start: 1px solid var(--border-subtle, #f1f5f9);
      font-size: 0.75rem;
      color: var(--text-secondary, #64748b);
    }

    .drawer-phone {
      font-weight: 700;
      color: #f59e0b;
      text-decoration: none;
    }

    @keyframes fade-in {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    @keyframes slide-up {
      from { transform: translateY(100%); }
      to { transform: translateY(0); }
    }
  `]
})
export class MobileNavComponent {
  readonly localization = inject(LocalizationService);

  isDrawerOpen: boolean = false;

  readonly primaryItems = [
    { id: 'dashboard', label: 'الرئيسية', icon: 'grid_view', route: '/workspace/dashboard' },
    { id: 'nfc-station', label: 'المحطة', icon: 'contactless', route: '/workspace/nfc-station' },
    { id: 'attendance', label: 'التحضير', icon: 'fact_check', route: '/workspace/attendance' },
    { id: 'students', label: 'الطلاب', icon: 'groups', route: '/workspace/students' }
  ];

  readonly secondaryItems = [
    { id: 'cards', label: 'بطاقات NFC', icon: 'credit_card', route: '/workspace/cards' },
    { id: 'groups', label: 'المجموعات', icon: 'layers', route: '/workspace/groups' },
    { id: 'schedule', label: 'الجدول', icon: 'calendar_month', route: '/workspace/schedule' },
    { id: 'payments', label: 'الرسوم', icon: 'account_balance_wallet', route: '/workspace/payments' },
    { id: 'whatsapp', label: 'واتساب', icon: 'chat', route: '/workspace/whatsapp' },
    { id: 'lms', label: 'الامتحانات', icon: 'quiz', route: '/workspace/lms' },
    { id: 'reports', label: 'التقارير', icon: 'bar_chart', route: '/workspace/reports' },
    { id: 'team', label: 'الفريق', icon: 'verified_user', route: '/workspace/team' },
    { id: 'settings', label: 'الإعدادات', icon: 'settings', route: '/workspace/settings' },
    { id: 'ai-assistant', label: 'المساعد الذكي', icon: 'auto_awesome', route: '/workspace/ai-assistant' }
  ];

  toggleDrawer(): void {
    this.isDrawerOpen = !this.isDrawerOpen;
  }

  closeDrawer(): void {
    this.isDrawerOpen = false;
  }
}
