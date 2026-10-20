/**
 * Skeleton Loader Component
 * Milestone F1 Phase 7: Constitution Principle 7 (5 Interaction States) Pulse Loading Placeholder
 */

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type SkeletonShape = 'text' | 'card' | 'table-row' | 'circle';

@Component({
  selector: 'app-skeleton-loader',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="skeleton-container" [attr.aria-hidden]="true">
      @for (item of items; track $index) {
        <div
          class="skeleton-item"
          [ngClass]="'skeleton-' + type"
          [style.width]="width"
          [style.height]="itemHeight"
        ></div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
    }

    .skeleton-container {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      width: 100%;
    }

    .skeleton-item {
      background: linear-gradient(
        90deg,
        var(--bg-card, #ffffff) 0%,
        var(--border-subtle, #f1f5f9) 50%,
        var(--bg-card, #ffffff) 100%
      );
      background-size: 200% 100%;
      animation: pulse 1.5s ease-in-out infinite;
      border-radius: var(--radius-sm, 6px);
    }

    [data-theme="dark"] .skeleton-item {
      background: linear-gradient(
        90deg,
        var(--bg-card, #131c31) 0%,
        var(--border-subtle, #1e293b) 50%,
        var(--bg-card, #131c31) 100%
      );
      background-size: 200% 100%;
    }

    .skeleton-text {
      border-radius: var(--radius-sm, 4px);
    }

    .skeleton-card {
      border-radius: var(--radius-md, 8px);
      min-height: 4.5rem;
    }

    .skeleton-table-row {
      border-radius: var(--radius-sm, 4px);
      min-height: 2.5rem;
    }

    .skeleton-circle {
      border-radius: 50%;
      flex-shrink: 0;
    }

    @keyframes pulse {
      0% {
        background-position: 200% 0;
      }
      100% {
        background-position: -200% 0;
      }
    }

    @media (prefers-reduced-motion: reduce) {
      .skeleton-item {
        animation: none;
        opacity: 0.7;
      }
    }
  `]
})
export class SkeletonLoaderComponent {
  @Input() type: SkeletonShape = 'text';
  @Input() width: string = '100%';
  @Input() height?: string;
  @Input() count: number = 1;

  get items(): number[] {
    const validCount = Math.max(1, Math.floor(this.count || 1));
    return Array.from({ length: validCount }, (_, i) => i);
  }

  get itemHeight(): string {
    if (this.height) return this.height;
    switch (this.type) {
      case 'circle':
        return this.width;
      case 'card':
        return '4.5rem';
      case 'table-row':
        return '2.5rem';
      case 'text':
      default:
        return '1rem';
    }
  }
}
