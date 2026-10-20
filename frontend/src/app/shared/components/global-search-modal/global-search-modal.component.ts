import {
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationService } from '../../../core/services/localization.service';

@Component({
  selector: 'app-global-search-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (isOpen) {
      <div class="search-backdrop" (click)="onBackdropClick($event)">
        <div
          #dialogContainer
          class="search-dialog"
          role="dialog"
          aria-modal="true"
          aria-labelledby="search-dialog-title"
        >
          <!-- Search Input Header -->
          <div class="search-header">
            <span class="material-symbols-outlined search-leading-icon">search</span>
            <input
              #searchInput
              type="text"
              id="global-search-input"
              [(ngModel)]="searchQuery"
              class="search-input"
              [placeholder]="localization.translate('SHELL.SEARCH_PLACEHOLDER') || 'بحث سريع (Ctrl+K)...'"
              aria-label="البحث الشامل"
              autocomplete="off"
            />
            <div class="header-end-actions">
              <kbd class="shortcut-pill">ESC</kbd>
              <button
                type="button"
                class="btn-close-search"
                (click)="closeModal()"
                [attr.aria-label]="localization.translate('SHELL.SEARCH_MODAL_CLOSE') || 'إغلاق'"
              >
                <span class="material-symbols-outlined">close</span>
              </button>
            </div>
          </div>

          <!-- Informational Notice Badge for Milestone F3 -->
          <div class="search-body">
            <div class="info-notice-banner" role="status">
              <span class="material-symbols-outlined notice-icon">info</span>
              <div class="notice-text-wrapper">
                <span id="search-dialog-title" class="sr-only">
                  {{ localization.translate('SHELL.SEARCH_MODAL_TITLE') || 'البحث الشامل' }}
                </span>
                <p class="notice-message">
                  {{
                    localization.isRtl()
                      ? 'البحث الشامل — سيتم ربطه بسجل الطلاب والمجموعات في المرحلة F3'
                      : 'Global Search — Integration with the Students Directory and Groups will be available in Milestone F3.'
                  }}
                </p>
              </div>
            </div>

            <!-- Empty / Typing Placeholder State -->
            @if (searchQuery.trim().length > 0) {
              <div class="query-echo-box">
                <span class="echo-label">جاري البحث عن:</span>
                <span class="echo-query">«{{ searchQuery }}»</span>
                <p class="echo-notice">
                  {{
                    localization.isRtl()
                      ? 'سيتم تفعيل فهرس البحث والنتائج الحية في المرحلة F3.'
                      : 'Search indexing and live results will be activated in Milestone F3.'
                  }}
                </p>
              </div>
            } @else {
              <div class="search-empty-tip">
                <span class="tip-kbd-group">
                  استخدم <kbd class="tip-kbd">Ctrl</kbd> + <kbd class="tip-kbd">K</kbd> لفتح البحث السريع من أي مكان
                </span>
              </div>
            }
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    :host {
      display: contents;
    }

    .sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    }

    .search-backdrop {
      position: fixed;
      inset: 0;
      z-index: 60;
      display: flex;
      align-items: flex-start;
      justify-content: center;
      padding-block-start: 12vh;
      padding-inline: 1rem;
      background-color: rgba(15, 23, 42, 0.6);
      backdrop-filter: blur(4px);
      animation: backdrop-fade 0.15s ease-out;
    }

    .search-dialog {
      background-color: var(--bg-surface, #ffffff);
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: var(--radius-lg, 16px);
      width: 100%;
      max-width: 40rem;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
      overflow: hidden;
      animation: dialog-pop 0.2s cubic-bezier(0.16, 1, 0.3, 1);
      display: flex;
      flex-direction: column;
    }

    .search-header {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding-block: 0.875rem;
      padding-inline: 1.25rem;
      border-block-end: 1px solid var(--border-subtle, #e2e8f0);
      background-color: var(--bg-surface, #ffffff);
    }

    .search-leading-icon {
      color: var(--brand-accent, #3b82f6);
      font-size: 1.5rem;
      flex-shrink: 0;
    }

    .search-input {
      flex: 1;
      border: none;
      outline: none;
      background: transparent;
      font-size: 1.0625rem;
      color: var(--text-primary, #0f172a);
      line-height: 1.5;
    }

    .search-input::placeholder {
      color: var(--text-muted, #94a3b8);
    }

    .header-end-actions {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      flex-shrink: 0;
    }

    .shortcut-pill {
      font-family: monospace;
      font-size: 0.6875rem;
      font-weight: 700;
      color: var(--text-secondary, #64748b);
      background-color: var(--bg-surface-elevated, #f1f5f9);
      border: 1px solid var(--border-subtle, #cbd5e1);
      border-radius: 6px;
      padding-block: 0.125rem;
      padding-inline: 0.375rem;
    }

    .btn-close-search {
      background: transparent;
      border: none;
      cursor: pointer;
      color: var(--text-muted, #64748b);
      border-radius: 6px;
      padding: 4px;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: background-color 0.15s ease;
    }

    .btn-close-search:hover {
      background-color: var(--bg-surface-elevated, #f1f5f9);
      color: var(--text-primary, #0f172a);
    }

    .btn-close-search:focus-visible {
      outline: 3px solid var(--focus-ring, #3b82f6);
    }

    .search-body {
      padding-block: 1.25rem;
      padding-inline: 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .info-notice-banner {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      background-color: rgba(59, 130, 246, 0.08);
      border: 1px solid rgba(59, 130, 246, 0.25);
      border-radius: var(--radius-md, 10px);
      padding: 0.875rem 1rem;
      text-align: start;
    }

    .notice-icon {
      color: var(--brand-accent, #3b82f6);
      font-size: 1.25rem;
      flex-shrink: 0;
      margin-block-start: 1px;
    }

    .notice-message {
      font-size: 0.8125rem;
      font-weight: 600;
      color: var(--brand-primary, #1e3a8a);
      line-height: 1.5;
      margin: 0;
    }

    .query-echo-box {
      background-color: var(--bg-surface-elevated, #f8fafc);
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: var(--radius-md, 10px);
      padding: 1.25rem;
      text-align: center;
      display: flex;
      flex-direction: column;
      gap: 0.375rem;
    }

    .echo-label {
      font-size: 0.75rem;
      color: var(--text-secondary, #64748b);
    }

    .echo-query {
      font-size: 1.125rem;
      font-weight: 700;
      color: var(--text-primary, #0f172a);
    }

    .echo-notice {
      font-size: 0.75rem;
      color: var(--text-muted, #94a3b8);
      margin: 0;
      margin-block-start: 0.5rem;
    }

    .search-empty-tip {
      padding-block: 1rem;
      text-align: center;
      color: var(--text-muted, #94a3b8);
      font-size: 0.8125rem;
    }

    .tip-kbd-group {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
    }

    .tip-kbd {
      font-family: monospace;
      font-size: 0.6875rem;
      background-color: var(--bg-surface-elevated, #f1f5f9);
      border: 1px solid var(--border-subtle, #cbd5e1);
      border-radius: 4px;
      padding-block: 0.125rem;
      padding-inline: 0.375rem;
    }

    @keyframes backdrop-fade {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    @keyframes dialog-pop {
      from { opacity: 0; transform: translateY(-10px) scale(0.98); }
      to { opacity: 1; transform: translateY(0) scale(1); }
    }
  `]
})
export class GlobalSearchModalComponent implements OnInit, OnDestroy {
  readonly localization = inject(LocalizationService);

  @Input() isOpen: boolean = false;
  @Output() isOpenChange = new EventEmitter<boolean>();
  @Output() closed = new EventEmitter<void>();

  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;
  @ViewChild('dialogContainer') dialogContainer?: ElementRef<HTMLElement>;

  searchQuery: string = '';
  private previouslyFocusedElement: HTMLElement | null = null;
  private keydownListener?: (event: KeyboardEvent) => void;

  ngOnInit(): void {
    this.registerGlobalShortcut();
  }

  ngOnDestroy(): void {
    if (this.keydownListener) {
      window.removeEventListener('keydown', this.keydownListener);
    }
  }

  private registerGlobalShortcut(): void {
    this.keydownListener = (event: KeyboardEvent) => {
      // Check for Ctrl+K or Meta+K
      const isModifierK = (event.ctrlKey || event.metaKey) && (event.key === 'k' || event.key === 'K');

      if (isModifierK) {
        // Suppress shortcut inside inputs, textareas, or contenteditables
        const target = event.target as HTMLElement | null;
        const tagName = target?.tagName?.toLowerCase();
        const isEditable = target?.isContentEditable || tagName === 'input' || tagName === 'textarea' || tagName === 'select';

        if (isEditable && !this.isOpen) {
          return;
        }

        event.preventDefault();

        if (this.isOpen) {
          this.closeModal();
        } else {
          this.openModal();
        }
        return;
      }

      if (this.isOpen) {
        if (event.key === 'Escape') {
          event.preventDefault();
          this.closeModal();
          return;
        }

        if (event.key === 'Tab') {
          this.trapFocus(event);
        }
      }
    };

    window.addEventListener('keydown', this.keydownListener);
  }

  openModal(): void {
    this.previouslyFocusedElement = document.activeElement as HTMLElement | null;
    this.isOpen = true;
    this.isOpenChange.emit(true);

    setTimeout(() => {
      this.searchInput?.nativeElement?.focus();
    }, 50);
  }

  closeModal(): void {
    this.isOpen = false;
    this.isOpenChange.emit(false);
    this.closed.emit();
    this.searchQuery = '';

    if (this.previouslyFocusedElement && typeof this.previouslyFocusedElement.focus === 'function') {
      setTimeout(() => {
        this.previouslyFocusedElement?.focus();
      }, 50);
    }
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('search-backdrop')) {
      this.closeModal();
    }
  }

  private trapFocus(event: KeyboardEvent): void {
    if (!this.dialogContainer?.nativeElement) return;

    const focusables = this.dialogContainer.nativeElement.querySelectorAll<HTMLElement>(
      'button, input, [tabindex]:not([tabindex="-1"])'
    );
    if (!focusables || focusables.length === 0) return;

    const first = focusables[0];
    const last = focusables[focusables.length - 1];

    if (event.shiftKey && document.activeElement === first) {
      last.focus();
      event.preventDefault();
    } else if (!event.shiftKey && document.activeElement === last) {
      first.focus();
      event.preventDefault();
    }
  }
}
