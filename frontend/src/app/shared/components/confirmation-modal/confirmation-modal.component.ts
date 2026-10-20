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
  selector: 'app-confirmation-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (isOpen) {
      <div class="modal-backdrop" (click)="onBackdropClick($event)">
        <div
          #dialogContainer
          class="modal-dialog"
          role="alertdialog"
          aria-modal="true"
          [attr.aria-labelledby]="'confirm-modal-title'"
          [attr.aria-describedby]="'confirm-modal-desc'"
        >
          <div class="modal-header">
            <h3 id="confirm-modal-title" class="modal-title" [class.text-danger]="isDestructive">
              {{ title }}
            </h3>
            <button
              type="button"
              class="close-btn"
              (click)="onCancel()"
              aria-label="إغلاق النافذة"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>

          <div class="modal-body">
            <p id="confirm-modal-desc" class="modal-desc">{{ message }}</p>

            @if (requireReason) {
              <div class="reason-group">
                <label for="action-reason" class="reason-label">
                  سبب الإجراء (إلزامي - {{ reasonMinLength }} أحرف على الأقل):
                </label>
                <textarea
                  id="action-reason"
                  class="reason-textarea"
                  rows="3"
                  [(ngModel)]="reason"
                  placeholder="اكتب سبب هذا الإجراء هنا..."
                  [class.invalid]="isReasonInvalid"
                ></textarea>
                @if (isReasonInvalid) {
                  <span class="reason-error">
                    يرجى إدخال {{ reasonMinLength - (reason ? reason.trim().length : 0) }} حرف إضافي على الأقل للمتابعة.
                  </span>
                }
              </div>
            }
          </div>

          <div class="modal-footer">
            <button
              type="button"
              class="btn-cancel"
              (click)="onCancel()"
            >
              {{ cancelButtonText || 'إلغاء' }}
            </button>

            <button
              type="button"
              class="btn-confirm"
              [ngClass]="isDestructive ? 'btn-danger' : 'btn-primary'"
              [disabled]="isConfirmDisabled"
              (click)="onConfirm()"
            >
              {{ confirmButtonText || 'تأكيد' }}
            </button>
          </div>
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
      background-color: rgba(15, 23, 42, 0.65);
      backdrop-filter: blur(4px);
      padding: 1rem;
    }

    .modal-dialog {
      background-color: var(--bg-surface, #ffffff);
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: var(--radius-lg, 16px);
      width: 100%;
      max-width: 32rem;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1);
      display: flex;
      flex-direction: column;
      overflow: hidden;
      animation: modal-enter 0.2s ease-out;
    }

    .modal-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding-block: 1.25rem;
      padding-inline: 1.5rem;
      border-block-end: 1px solid var(--border-subtle, #e2e8f0);
    }

    .modal-title {
      font-size: 1.125rem;
      font-weight: 700;
      color: var(--text-primary, #0f172a);
      margin: 0;
    }

    .text-danger {
      color: var(--color-danger, #ef4444);
    }

    .close-btn {
      background: transparent;
      border: none;
      cursor: pointer;
      color: var(--text-muted, #64748b);
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 6px;
      padding: 4px;
    }

    .close-btn:focus-visible {
      outline: 3px solid var(--focus-ring, #3b82f6);
    }

    .modal-body {
      padding-block: 1.5rem;
      padding-inline: 1.5rem;
    }

    .modal-desc {
      font-size: 0.9375rem;
      color: var(--text-secondary, #475569);
      margin: 0;
      line-height: 1.6;
    }

    .reason-group {
      margin-block-start: 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .reason-label {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--text-primary, #0f172a);
    }

    .reason-textarea {
      width: 100%;
      border: 1px solid var(--border-strong, #cbd5e1);
      border-radius: var(--radius-md, 8px);
      padding: 0.625rem;
      font-size: 0.875rem;
      background-color: var(--bg-surface, #ffffff);
      color: var(--text-primary, #0f172a);
      outline: none;
      box-sizing: border-box;
      resize: vertical;
    }

    .reason-textarea:focus-visible {
      border-color: var(--brand-accent, #3b82f6);
      outline: 3px solid var(--focus-ring, #3b82f6);
      outline-offset: 1px;
    }

    .reason-textarea.invalid {
      border-color: var(--color-danger, #ef4444);
    }

    .reason-error {
      font-size: 0.75rem;
      color: var(--color-danger, #ef4444);
    }

    .modal-footer {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 0.75rem;
      padding-block: 1.25rem;
      padding-inline: 1.5rem;
      background-color: var(--bg-surface-elevated, #f8fafc);
      border-block-start: 1px solid var(--border-subtle, #e2e8f0);
    }

    .btn-cancel,
    .btn-confirm {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      padding-block: 0.625rem;
      padding-inline: 1.25rem;
      font-size: 0.875rem;
      font-weight: 600;
      border-radius: var(--radius-md, 8px);
      cursor: pointer;
      transition: background-color 0.15s ease;
      outline: none;
    }

    .btn-cancel:focus-visible,
    .btn-confirm:focus-visible {
      outline: 3px solid var(--focus-ring, #3b82f6);
      outline-offset: 2px;
    }

    .btn-cancel {
      background-color: transparent;
      border: 1px solid var(--border-strong, #cbd5e1);
      color: var(--text-primary, #0f172a);
    }

    .btn-confirm:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .btn-danger {
      background-color: var(--color-danger, #ef4444);
      color: #ffffff;
      border: 1px solid transparent;
    }

    .btn-danger:hover:not(:disabled) {
      background-color: #dc2626;
    }

    .btn-primary {
      background-color: var(--brand-primary, #1e3a8a);
      color: #ffffff;
      border: 1px solid transparent;
    }

    .btn-primary:hover:not(:disabled) {
      background-color: var(--brand-primary-hover, #172554);
    }

    @keyframes modal-enter {
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
export class ConfirmationModalComponent {
  @Input() isOpen: boolean = false;
  @Input() title: string = '';
  @Input() message: string = '';
  @Input() requireReason: boolean = false;
  @Input() reasonMinLength: number = 5;
  @Input() confirmButtonText: string = 'تأكيد';
  @Input() cancelButtonText: string = 'إلغاء';
  @Input() isDestructive: boolean = true;

  @Output() confirmed = new EventEmitter<string | void>();
  @Output() cancelled = new EventEmitter<void>();

  @ViewChild('dialogContainer') dialogContainer?: ElementRef<HTMLElement>;

  reason: string = '';

  @HostListener('window:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (!this.isOpen) return;

    if (event.key === 'Escape') {
      event.preventDefault();
      this.onCancel();
      return;
    }

    if (event.key === 'Tab') {
      this.trapFocus(event);
    }
  }

  get isReasonInvalid(): boolean {
    if (!this.requireReason) return false;
    return this.reason.trim().length > 0 && this.reason.trim().length < this.reasonMinLength;
  }

  get isConfirmDisabled(): boolean {
    if (!this.requireReason) return false;
    return (this.reason?.trim().length || 0) < this.reasonMinLength;
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-backdrop')) {
      this.onCancel();
    }
  }

  onConfirm(): void {
    if (this.isConfirmDisabled) return;
    this.confirmed.emit(this.requireReason ? this.reason.trim() : undefined);
    this.reset();
  }

  onCancel(): void {
    this.cancelled.emit();
    this.reset();
  }

  private reset(): void {
    this.reason = '';
    this.isOpen = false;
  }

  private trapFocus(event: KeyboardEvent): void {
    if (!this.dialogContainer?.nativeElement) return;

    const focusables = this.dialogContainer.nativeElement.querySelectorAll<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
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
