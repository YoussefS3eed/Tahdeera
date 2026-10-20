import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-workspace-placeholder',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="placeholder-container">
      <div class="placeholder-card">
        <div class="icon-bubble">
          <span class="material-symbols-outlined">dashboard_customize</span>
        </div>

        <h2 class="placeholder-title">
          مساحة العمل — سيتم تفعيل لوحة التحكم في المرحلة 4 وباقي الأقسام في المراحل اللاحقة
        </h2>

        <p class="placeholder-desc">
          تم تثبيت هيكل نظام تحضيرة ومخطط القوائم الجانبية الموحدة بنجاح. يمكنك استعراض التجاوب وتغيير اللغة والمظهر، وسيتم تفعيل لوحة المؤشرات (Dashboard) ومحطة التحضير في المراحل القادمة.
        </p>

        <div class="badge-notice">
          <span class="material-symbols-outlined notice-icon">info</span>
          <span>المرحلة الحالية: تثبيت قوالب العرض وبوابة الهبوط (Phases 1–3)</span>
        </div>

        <div class="placeholder-actions">
          <button type="button" class="btn-return" (click)="navigateHome()">
            <span class="material-symbols-outlined">arrow_forward</span>
            العودة لبوابة الزوار
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
      padding: 2rem 1rem;
      width: 100%;
      max-width: 36rem;
      margin-inline: auto;
    }

    .placeholder-card {
      background-color: var(--bg-surface, #ffffff);
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: var(--radius-lg, 16px);
      padding: 2.5rem;
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
      padding-block: 0.5rem;
      padding-inline: 1rem;
      border-radius: 9999px;
      background-color: var(--bg-surface-elevated, #f1f5f9);
      color: var(--text-secondary, #475569);
      font-size: 0.8125rem;
      font-weight: 600;
      margin-block-end: 2rem;
    }

    .notice-icon {
      font-size: 1rem;
      color: var(--brand-accent, #3b82f6);
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
  constructor(private router: Router) {}

  navigateHome(): void {
    this.router.navigate(['/']);
  }
}
