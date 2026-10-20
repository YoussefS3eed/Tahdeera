import {
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  Output,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-reauth-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (isOpen) {
      <div class="modal-backdrop">
        <div
          #dialogContainer
          class="modal-dialog"
          role="dialog"
          aria-modal="true"
          aria-labelledby="reauth-modal-title"
        >
          <div class="modal-header">
            <div class="icon-badge">
              <span class="material-symbols-outlined">lock</span>
            </div>
            <h3 id="reauth-modal-title" class="modal-title">
              تأكيد الهوية للمتابعة
            </h3>
            <p class="modal-subtitle">
              انتهت صلاحية جلستك الحالية. أدخل كلمة المرور للمتابعة فوراً دون فقدان أي بيانات:
            </p>
          </div>

          <form (ngSubmit)="onSubmit()" class="modal-form">
            <div class="form-group">
              <label for="reauth-email" class="form-label">البريد الإلكتروني</label>
              <input
                id="reauth-email"
                type="email"
                class="form-input disabled"
                [value]="userEmail"
                disabled
              />
            </div>

            <div class="form-group">
              <label for="reauth-password" class="form-label">كلمة المرور</label>
              <div class="password-wrapper">
                <input
                  #passwordInput
                  id="reauth-password"
                  [type]="showPassword ? 'text' : 'password'"
                  class="form-input password-input"
                  [(ngModel)]="password"
                  name="password"
                  placeholder="••••••••"
                  required
                  autofocus
                />
                <button
                  type="button"
                  class="toggle-pwd-btn"
                  (click)="togglePasswordVisibility()"
                  [attr.aria-label]="showPassword ? 'إخفاء كلمة المرور' : 'إظهار كلمة المرور'"
                >
                  <span class="material-symbols-outlined">
                    {{ showPassword ? 'visibility_off' : 'visibility' }}
                  </span>
                </button>
              </div>
            </div>

            @if (errorMessage) {
              <div class="error-alert" role="alert">
                <span class="material-symbols-outlined error-icon">error</span>
                <span>{{ errorMessage }}</span>
              </div>
            }

            <div class="modal-footer">
              <button
                type="button"
                class="btn-cancel"
                (click)="onClose()"
              >
                إلغاء
              </button>

              <button
                type="submit"
                class="btn-primary"
                [disabled]="!password || isLoading"
              >
                @if (isLoading) {
                  <span class="loading-spinner"></span>
                }
                تأكيد واستمرار
              </button>
            </div>
          </form>
        </div>
      </div>
    }
  `,
  styles: [`
    :host {
      display: contents;
    }

    .modal-backdrop {
      position: fixed;
      inset: 0;
      z-index: 50;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: rgba(15, 23, 42, 0.7);
      backdrop-filter: blur(6px);
      padding: 1rem;
    }

    .modal-dialog {
      background-color: var(--bg-surface, #ffffff);
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: var(--radius-lg, 16px);
      width: 100%;
      max-width: 28rem;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.15);
      padding: 1.5rem;
      animation: modal-scale 0.2s ease-out;
    }

    .modal-header {
      text-align: center;
      margin-block-end: 1.5rem;
    }

    .icon-badge {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 3.5rem;
      height: 3.5rem;
      border-radius: 50%;
      background-color: var(--brand-accent-bg, #eff6ff);
      color: var(--brand-accent, #3b82f6);
      margin-block-end: 0.75rem;
    }

    .modal-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--text-primary, #0f172a);
      margin: 0;
      margin-block-end: 0.375rem;
    }

    .modal-subtitle {
      font-size: 0.875rem;
      color: var(--text-secondary, #475569);
      margin: 0;
      line-height: 1.5;
    }

    .form-group {
      margin-block-end: 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.375rem;
    }

    .form-label {
      font-size: 0.8125rem;
      font-weight: 600;
      color: var(--text-primary, #0f172a);
    }

    .form-input {
      width: 100%;
      padding-block: 0.625rem;
      padding-inline: 0.875rem;
      font-size: 0.875rem;
      border: 1px solid var(--border-strong, #cbd5e1);
      border-radius: var(--radius-md, 8px);
      background-color: var(--bg-surface, #ffffff);
      color: var(--text-primary, #0f172a);
      outline: none;
      box-sizing: border-box;
      transition: border-color 0.15s ease;
    }

    .form-input.disabled {
      background-color: var(--bg-surface-elevated, #f1f5f9);
      color: var(--text-muted, #64748b);
      cursor: not-allowed;
    }

    .password-wrapper {
      position: relative;
      display: flex;
      align-items: center;
    }

    .password-input {
      padding-inline-end: 2.75rem;
    }

    .password-input:focus-visible {
      border-color: var(--brand-accent, #3b82f6);
      outline: 3px solid var(--focus-ring, #3b82f6);
      outline-offset: 1px;
    }

    .toggle-pwd-btn {
      position: absolute;
      inset-inline-end: 0.5rem;
      background: transparent;
      border: none;
      cursor: pointer;
      color: var(--text-muted, #64748b);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 4px;
      border-radius: 4px;
    }

    .toggle-pwd-btn:focus-visible {
      outline: 2px solid var(--focus-ring, #3b82f6);
    }

    .error-alert {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.625rem;
      border-radius: var(--radius-md, 8px);
      background-color: #fef2f2;
      border: 1px solid #fecaca;
      color: var(--color-danger, #ef4444);
      font-size: 0.8125rem;
      margin-block-end: 1rem;
    }

    .error-icon {
      font-size: 1.125rem;
      flex-shrink: 0;
    }

    .modal-footer {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 0.75rem;
      margin-block-start: 1.5rem;
    }

    .btn-cancel,
    .btn-primary {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding-block: 0.625rem;
      padding-inline: 1.25rem;
      font-size: 0.875rem;
      font-weight: 600;
      border-radius: var(--radius-md, 8px);
      cursor: pointer;
      outline: none;
      transition: background-color 0.15s ease;
    }

    .btn-cancel:focus-visible,
    .btn-primary:focus-visible {
      outline: 3px solid var(--focus-ring, #3b82f6);
      outline-offset: 2px;
    }

    .btn-cancel {
      background-color: transparent;
      border: 1px solid var(--border-strong, #cbd5e1);
      color: var(--text-primary, #0f172a);
    }

    .btn-primary {
      background-color: var(--brand-primary, #1e3a8a);
      color: #ffffff;
      border: 1px solid transparent;
    }

    .btn-primary:hover:not(:disabled) {
      background-color: var(--brand-primary-hover, #172554);
    }

    .btn-primary:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .loading-spinner {
      width: 1rem;
      height: 1rem;
      border: 2px solid #ffffff;
      border-block-start-color: transparent;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
      to {
        transform: rotate(360deg);
      }
    }

    @keyframes modal-scale {
      from {
        opacity: 0;
        transform: scale(0.95);
      }
      to {
        opacity: 1;
        transform: scale(1);
      }
    }
  `]
})
export class ReauthModalComponent {
  @Input() isOpen: boolean = false;
  @Input() userEmail: string = '';
  @Input() errorMessage: string = '';
  @Input() isLoading: boolean = false;

  @Output() submitPassword = new EventEmitter<string>();
  @Output() closed = new EventEmitter<void>();

  @ViewChild('dialogContainer') dialogContainer?: ElementRef<HTMLElement>;
  @ViewChild('passwordInput') passwordInput?: ElementRef<HTMLInputElement>;

  password: string = '';
  showPassword: boolean = false;

  @HostListener('window:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (!this.isOpen) return;

    if (event.key === 'Escape') {
      event.preventDefault();
      this.onClose();
      return;
    }

    if (event.key === 'Tab') {
      this.trapFocus(event);
    }
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    if (!this.password || this.isLoading) return;
    this.submitPassword.emit(this.password);
  }

  onClose(): void {
    this.password = '';
    this.isOpen = false;
    this.closed.emit();
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
