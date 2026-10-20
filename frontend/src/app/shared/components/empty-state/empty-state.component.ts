import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EmptyStateMode } from '../../models/ui-primitives.models';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="empty-state-card" [attr.data-mode]="mode">
      <div class="empty-state-icon-wrapper">
        @if (mode === 'zero-match') {
          <span class="material-symbols-outlined empty-state-icon">{{ iconName || 'search_off' }}</span>
        } @else {
          <span class="material-symbols-outlined empty-state-icon">{{ iconName || 'inbox' }}</span>
        }
      </div>

      <h3 class="empty-state-title">{{ title }}</h3>
      
      @if (description) {
        <p class="empty-state-desc">{{ description }}</p>
      }

      @if (actionLabel) {
        <button
          type="button"
          class="empty-state-btn"
          [ngClass]="mode === 'zero-match' ? 'btn-secondary' : 'btn-primary'"
          (click)="onActionClick()"
        >
          {{ actionLabel }}
        </button>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
    }

    .empty-state-card {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
      padding-block: 3rem;
      padding-inline: 1.5rem;
      background-color: var(--bg-surface, #ffffff);
      border: 1px dashed var(--border-subtle, #e2e8f0);
      border-radius: var(--radius-lg, 16px);
      transition: background-color 0.2s ease, border-color 0.2s ease;
    }

    .empty-state-icon-wrapper {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 4rem;
      height: 4rem;
      border-radius: 50%;
      background-color: var(--bg-surface-elevated, #f1f5f9);
      margin-block-end: 1rem;
      color: var(--text-muted, #64748b);
    }

    .empty-state-icon {
      font-size: 2.25rem;
    }

    .empty-state-title {
      font-size: 1.125rem;
      font-weight: 700;
      color: var(--text-primary, #0f172a);
      margin: 0;
      margin-block-end: 0.5rem;
    }

    .empty-state-desc {
      font-size: 0.875rem;
      color: var(--text-secondary, #475569);
      max-width: 28rem;
      margin: 0;
      margin-block-end: 1.5rem;
      line-height: 1.6;
    }

    .empty-state-btn {
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
      transition: background-color 0.15s ease, transform 0.1s ease;
      outline: none;
    }

    .empty-state-btn:focus-visible {
      outline: 3px solid var(--focus-ring, #3b82f6);
      outline-offset: 2px;
    }

    .empty-state-btn:active {
      transform: scale(0.98);
    }

    .btn-primary {
      background-color: var(--brand-primary, #1e3a8a);
      color: #ffffff;
      border: 1px solid transparent;
    }

    .btn-primary:hover {
      background-color: var(--brand-primary-hover, #172554);
    }

    .btn-secondary {
      background-color: transparent;
      color: var(--text-primary, #0f172a);
      border: 1px solid var(--border-strong, #cbd5e1);
    }

    .btn-secondary:hover {
      background-color: var(--bg-surface-elevated, #f8fafc);
    }
  `]
})
export class EmptyStateComponent {
  @Input() mode: EmptyStateMode = 'zero-data';
  @Input() title: string = '';
  @Input() description: string = '';
  @Input() iconName?: string;
  @Input() actionLabel: string = '';

  @Output() actionClicked = new EventEmitter<void>();

  onActionClick(): void {
    this.actionClicked.emit();
  }
}
