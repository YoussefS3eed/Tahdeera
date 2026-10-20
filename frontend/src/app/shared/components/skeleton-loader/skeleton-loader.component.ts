/**
 * Skeleton Loader Component
 * Milestone F2 / Principle 7 & Principle 2: Fixed-dimension pulse loading placeholder
 * Guarantees Cumulative Layout Shift (CLS) strictly < 0.05
 */

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SkeletonVariant } from '../../models/ui-primitives.models';

export type SkeletonShape = 'text' | 'card' | 'table-row' | 'circle' | SkeletonVariant;

@Component({
  selector: 'app-skeleton-loader',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="skeleton-container" [attr.aria-hidden]="true">
      @for (item of items; track $index) {
        <div
          class="skeleton-item"
          [ngClass]="activeClass"
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
      gap: 0.75rem;
      width: 100%;
    }

    .skeleton-item {
      background: linear-gradient(
        90deg,
        var(--bg-surface-elevated, var(--bg-card, #ffffff)) 0%,
        var(--border-subtle, #f1f5f9) 50%,
        var(--bg-surface-elevated, var(--bg-card, #ffffff)) 100%
      );
      background-size: 200% 100%;
      animation: skeleton-pulse 1.5s ease-in-out infinite;
      border-radius: var(--radius-sm, 6px);
    }

    [data-theme="dark"] .skeleton-item {
      background: linear-gradient(
        90deg,
        var(--bg-surface-elevated, #1e293b) 0%,
        var(--border-subtle, #334155) 50%,
        var(--bg-surface-elevated, #1e293b) 100%
      );
      background-size: 200% 100%;
    }

    .skeleton-kpi-card,
    .skeleton-card {
      border-radius: 16px;
      min-height: 124px;
      height: 124px;
    }

    .skeleton-table-row {
      border-radius: 6px;
      min-height: 52px;
      height: 52px;
    }

    .skeleton-text-line,
    .skeleton-text {
      border-radius: 4px;
      min-height: 1rem;
    }

    .skeleton-avatar,
    .skeleton-circle {
      border-radius: 50%;
      flex-shrink: 0;
    }

    @keyframes skeleton-pulse {
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
  @Input() variant: SkeletonVariant = 'kpi-card';
  @Input() type?: SkeletonShape;
  @Input() width: string = '100%';
  @Input() height?: string;
  @Input() count: number = 1;

  get resolvedType(): string {
    return this.type || this.variant || 'kpi-card';
  }

  get activeClass(): string {
    const t = this.resolvedType;
    if (t === 'kpi-card' || t === 'card') return 'skeleton-kpi-card';
    if (t === 'table-row') return 'skeleton-table-row';
    if (t === 'avatar' || t === 'circle') return 'skeleton-avatar';
    return 'skeleton-text-line';
  }

  get items(): number[] {
    const validCount = Math.max(1, Math.floor(this.count || 1));
    return Array.from({ length: validCount }, (_, i) => i);
  }

  get itemHeight(): string {
    if (this.height) return this.height;
    const t = this.resolvedType;
    switch (t) {
      case 'avatar':
      case 'circle':
        return this.width === '100%' ? '40px' : this.width;
      case 'kpi-card':
      case 'card':
        return '124px';
      case 'table-row':
        return '52px';
      case 'text-line':
      case 'text':
      default:
        return '1rem';
    }
  }
}
