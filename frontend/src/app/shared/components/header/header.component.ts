import { Component, EventEmitter, HostListener, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LocalizationService } from '../../../core/services/localization.service';
import { ThemeService } from '../../../core/services/theme.service';
import { GlobalSearchModalComponent } from '../global-search-modal/global-search-modal.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, GlobalSearchModalComponent],
  template: `
    <header class="workspace-header">
      <div class="header-start-group">
        <!-- Mobile Sidebar Toggle -->
        <button
          type="button"
          class="mobile-menu-btn"
          (click)="toggleSidebar.emit()"
          aria-label="القائمة الجانبية"
        >
          <span class="material-symbols-outlined">menu</span>
        </button>

        <!-- Workspace Title / Branch Badge -->
        <div class="workspace-badge">
          <span class="workspace-dot"></span>
          <span class="workspace-name">{{ workspaceName }}</span>
        </div>

        <!-- Global Search Button (Desktop) -->
        <button
          type="button"
          class="header-search-btn"
          (click)="openSearchModal()"
          [title]="localization.translate('SHELL.SEARCH_TOOLTIP') || 'البحث في سجل الطلاب والمجموعات'"
          aria-label="البحث الشامل"
        >
          <span class="material-symbols-outlined search-icon">search</span>
          <span class="search-placeholder-text">
            {{ localization.translate('SHELL.SEARCH_PLACEHOLDER') || 'بحث سريع (Ctrl+K)...' }}
          </span>
          <kbd class="search-kbd-hint">Ctrl+K</kbd>
        </button>
      </div>

      <div class="header-end-group">
        <!-- Mobile Search Button -->
        <button
          type="button"
          class="icon-btn mobile-search-btn"
          (click)="openSearchModal()"
          aria-label="البحث"
        >
          <span class="material-symbols-outlined">search</span>
        </button>

        <!-- Sync Status Pill -->
        <div class="sync-status-pill" title="حالة الاتصال والمزامنة">
          <span class="sync-indicator-dot"></span>
          <span class="sync-text">متصل</span>
        </div>

        <!-- Language Switcher (0ms reload) -->
        <button
          type="button"
          class="icon-btn"
          (click)="localization.toggleLanguage()"
          [title]="localization.isRtl() ? 'Switch to English' : 'التحويل إلى العربية'"
          aria-label="تغيير اللغة"
        >
          <span class="lang-text font-bold">{{ localization.isRtl() ? 'EN' : 'عربي' }}</span>
        </button>

        <!-- Theme Switcher -->
        <button
          type="button"
          class="icon-btn"
          (click)="theme.toggleTheme()"
          [title]="theme.isDarkMode() ? 'الوضع النهاري' : 'الوضع الليلي'"
          aria-label="تبديل المظهر"
        >
          <span class="material-symbols-outlined">
            {{ theme.isDarkMode() ? 'light_mode' : 'dark_mode' }}
          </span>
        </button>

        <!-- User Profile Avatar Initials Stub -->
        <div class="user-avatar-stub" [title]="userName">
          <span class="user-initials">{{ userInitials }}</span>
        </div>
      </div>

      <!-- Embedded Accessible Global Search Modal -->
      <app-global-search-modal
        [(isOpen)]="isSearchOpen"
      ></app-global-search-modal>
    </header>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
    }

    .workspace-header {
      height: 4rem;
      background-color: var(--bg-surface, #ffffff);
      border-block-end: 1px solid var(--border-subtle, #e2e8f0);
      padding-inline: 1.25rem;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      position: sticky;
      inset-block-start: 0;
      z-index: 30;
    }

    .header-start-group {
      display: flex;
      align-items: center;
      gap: 1rem;
      flex: 1;
      min-width: 0;
    }

    .mobile-menu-btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      background: transparent;
      border: none;
      color: var(--text-secondary, #475569);
      cursor: pointer;
      padding: 0.375rem;
      border-radius: var(--radius-md, 8px);
    }

    @media (min-width: 769px) {
      .mobile-menu-btn {
        display: none;
      }
    }

    .workspace-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding-block: 0.375rem;
      padding-inline: 0.75rem;
      background-color: var(--bg-surface-elevated, #f1f5f9);
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: 9999px;
      font-size: 0.8125rem;
      font-weight: 700;
      color: var(--text-primary, #0f172a);
      white-space: nowrap;
    }

    .workspace-dot {
      width: 0.5rem;
      height: 0.5rem;
      border-radius: 50%;
      background-color: #10b981;
    }

    .header-search-btn {
      display: none;
      align-items: center;
      gap: 0.625rem;
      width: 100%;
      max-width: 22rem;
      padding-block: 0.5rem;
      padding-inline: 0.875rem;
      background-color: var(--bg-surface-elevated, #f8fafc);
      border: 1px solid var(--border-subtle, #cbd5e1);
      border-radius: var(--radius-md, 10px);
      color: var(--text-muted, #94a3b8);
      cursor: pointer;
      font-size: 0.8125rem;
      transition: all 0.15s ease;
      text-align: start;
    }

    @media (min-width: 769px) {
      .header-search-btn {
        display: flex;
      }
    }

    .header-search-btn:hover {
      border-color: var(--brand-accent, #3b82f6);
      color: var(--text-primary, #0f172a);
    }

    .search-icon {
      font-size: 1.25rem;
      color: var(--text-secondary, #64748b);
    }

    .search-placeholder-text {
      flex: 1;
    }

    .search-kbd-hint {
      font-family: monospace;
      font-size: 0.6875rem;
      color: var(--text-secondary, #64748b);
      background-color: var(--bg-surface, #ffffff);
      border: 1px solid var(--border-subtle, #cbd5e1);
      border-radius: 4px;
      padding-block: 0.125rem;
      padding-inline: 0.375rem;
    }

    .header-end-group {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      flex-shrink: 0;
    }

    .mobile-search-btn {
      display: inline-flex;
    }

    @media (min-width: 769px) {
      .mobile-search-btn {
        display: none;
      }
    }

    .sync-status-pill {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding-block: 0.25rem;
      padding-inline: 0.625rem;
      background-color: rgba(16, 185, 129, 0.1);
      border: 1px solid rgba(16, 185, 129, 0.25);
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 700;
      color: #059669;
    }

    .sync-indicator-dot {
      width: 0.375rem;
      height: 0.375rem;
      border-radius: 50%;
      background-color: #10b981;
    }

    .icon-btn {
      background: transparent;
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: var(--radius-md, 8px);
      color: var(--text-secondary, #475569);
      padding: 0.5rem;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      min-width: 2.25rem;
      height: 2.25rem;
      transition: background-color 0.15s ease;
    }

    .icon-btn:hover {
      background-color: var(--bg-surface-elevated, #f1f5f9);
      color: var(--text-primary, #0f172a);
    }

    .lang-text {
      font-size: 0.8125rem;
    }

    .user-avatar-stub {
      width: 2.25rem;
      height: 2.25rem;
      border-radius: 50%;
      background-color: var(--brand-primary, #1e3a8a);
      color: #ffffff;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.8125rem;
      font-weight: 800;
      cursor: pointer;
      user-select: none;
    }

    .user-initials {
      line-height: 1;
    }
  `]
})
export class HeaderComponent {
  readonly localization = inject(LocalizationService);
  readonly theme = inject(ThemeService);

  @Input() workspaceName: string = 'سنتر الإتقان';
  @Input() userName: string = 'أحمد مصطفى';
  @Input() userInitials: string = 'أم';

  @Output() toggleSidebar = new EventEmitter<void>();

  isSearchOpen: boolean = false;

  @HostListener('window:keydown', ['$event'])
  handleKeyboardShortcut(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.openSearchModal();
    }
  }

  openSearchModal(): void {
    this.isSearchOpen = true;
  }
}
