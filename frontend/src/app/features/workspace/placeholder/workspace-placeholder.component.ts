import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { LocalizationService } from '../../../core/services/localization.service';
import { DEFERRED_WORKSPACE_TABS, DeferredTabMetadata, TargetMilestone } from './placeholder.models';

@Component({
  selector: 'app-workspace-placeholder',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="placeholder-container">
      <div class="placeholder-card">
        <div class="icon-bubble">
          <span class="material-symbols-outlined">{{ iconName() }}</span>
        </div>

        <h2 class="placeholder-title">
          {{ title() }}
        </h2>

        <p class="placeholder-desc">
          {{ description() }}
        </p>

        <div class="badge-notice" [ngClass]="badgeColorClass()">
          <span class="material-symbols-outlined notice-icon">rocket_launch</span>
          <span class="badge-label">{{ localization.translate('PLACEHOLDER.TARGET_MILESTONE') || 'المرحلة المجدولة:' }}</span>
          <span class="badge-value font-mono">{{ targetMilestone() }}</span>
        </div>

        <div class="placeholder-actions">
          <button type="button" class="btn-return" (click)="navigateDashboard()">
            <span class="material-symbols-outlined">arrow_forward</span>
            <span>{{ localization.translate('PLACEHOLDER.RETURN_TO_DASHBOARD') || 'العودة إلى لوحة التحكم' }}</span>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: flex;
      flex: 1;
      width: 100%;
      height: 100%;
      align-items: center;
      justify-content: center;
    }

    .placeholder-container {
      display: flex;
      align-items: center;
      justify-content: center;
      padding-block: 2rem;
      padding-inline: 1rem;
      width: 100%;
      max-width: 36rem;
      margin-inline: auto;
    }

    .placeholder-card {
      background-color: var(--bg-surface, #ffffff);
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: var(--radius-lg, 16px);
      padding-block: 2.5rem;
      padding-inline: 2rem;
      width: 100%;
      text-align: center;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
      display: flex;
      flex-direction: column;
      align-items: center;
    }

    .icon-bubble {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 4.5rem;
      height: 4.5rem;
      border-radius: 50%;
      background-color: var(--brand-accent-bg, #eff6ff);
      color: var(--brand-accent, #3b82f6);
      margin-block-end: 1.25rem;
    }

    .icon-bubble .material-symbols-outlined {
      font-size: 2.25rem;
    }

    .placeholder-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--text-primary, #0f172a);
      margin: 0;
      margin-block-end: 0.75rem;
      line-height: 1.5;
    }

    .placeholder-desc {
      font-size: 0.9375rem;
      color: var(--text-secondary, #475569);
      margin: 0;
      margin-block-end: 1.5rem;
      line-height: 1.6;
    }

    .badge-notice {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding-block: 0.375rem;
      padding-inline: 0.875rem;
      border-radius: 9999px;
      font-size: 0.8125rem;
      font-weight: 600;
      margin-block-end: 2rem;
      transition: background-color 0.15s ease, color 0.15s ease;
    }

    .badge-f3 {
      background-color: #eff6ff;
      color: #1d4ed8;
      border: 1px solid #bfdbfe;
    }

    :host-context([data-theme="dark"]) .badge-f3 {
      background-color: rgba(59, 130, 246, 0.15);
      color: #93c5fd;
      border-color: rgba(59, 130, 246, 0.3);
    }

    .badge-f4 {
      background-color: #faf5ff;
      color: #7e22ce;
      border: 1px solid #e9d5ff;
    }

    :host-context([data-theme="dark"]) .badge-f4 {
      background-color: rgba(168, 85, 247, 0.15);
      color: #d8b4fe;
      border-color: rgba(168, 85, 247, 0.3);
    }

    .notice-icon {
      font-size: 1.125rem;
    }

    .badge-value {
      font-weight: 700;
    }

    .placeholder-actions {
      display: flex;
      justify-content: center;
      width: 100%;
    }

    .btn-return {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding-block: 0.75rem;
      padding-inline: 1.75rem;
      font-size: 0.9375rem;
      font-weight: 600;
      border-radius: var(--radius-md, 8px);
      background-color: var(--brand-primary, #1e3a8a);
      color: #ffffff;
      border: 1px solid transparent;
      cursor: pointer;
      transition: background-color 0.15s ease, transform 0.1s ease;
      outline: none;
    }

    .btn-return:hover {
      background-color: var(--brand-primary-hover, #172554);
    }

    .btn-return:focus-visible {
      outline: 3px solid var(--focus-ring, #3b82f6);
      outline-offset: 2px;
    }
  `]
})
export class WorkspacePlaceholderComponent {
  readonly route = inject(ActivatedRoute);
  readonly router = inject(Router);
  readonly localization = inject(LocalizationService);

  private readonly fallbackMeta: DeferredTabMetadata = {
    routePath: 'unknown',
    iconName: 'construction',
    titleAr: 'قسم قيد التطوير',
    titleEn: 'Under Development',
    descriptionAr: 'يتم العمل على تطوير وتجهيز هذا القسم حاليًا وفقًا لخارطة طريق منصة تحضيرة.',
    descriptionEn: 'This section is currently being developed according to the Tahdeera platform roadmap.',
    targetMilestone: 'Milestone F3'
  };

  private readonly routePath = signal<string>(
    this.route.snapshot.url[0]?.path || this.route.snapshot.routeConfig?.path || ''
  );

  readonly currentTabMeta = computed<DeferredTabMetadata>(() => {
    const slug = this.routePath();
    return DEFERRED_WORKSPACE_TABS[slug] || this.fallbackMeta;
  });

  readonly title = computed<string>(() => {
    const meta = this.currentTabMeta();
    return this.localization.currentLang() === 'ar' ? meta.titleAr : meta.titleEn;
  });

  readonly description = computed<string>(() => {
    const meta = this.currentTabMeta();
    return this.localization.currentLang() === 'ar' ? meta.descriptionAr : meta.descriptionEn;
  });

  readonly targetMilestone = computed<TargetMilestone>(() => {
    return this.currentTabMeta().targetMilestone;
  });

  readonly iconName = computed<string>(() => {
    return this.currentTabMeta().iconName;
  });

  readonly badgeColorClass = computed<string>(() => {
    return this.targetMilestone() === 'Milestone F3' ? 'badge-f3' : 'badge-f4';
  });

  navigateDashboard(): void {
    this.router.navigate(['/workspace/dashboard']);
  }
}
