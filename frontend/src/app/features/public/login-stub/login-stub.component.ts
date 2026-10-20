import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-login-stub',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="login-stub-container">
      <div class="login-stub-card">
        <div class="brand-badge">
          <span class="brand-cap">🎓</span>
          <span class="brand-name">تحضيرة</span>
        </div>

        <div class="stub-icon-circle">
          <span class="material-symbols-outlined">login</span>
        </div>

        <h2 class="stub-title">
          صفحة تسجيل الدخول — سيتم تفعيلها بالكامل في المرحلة 4
        </h2>

        <p class="stub-description">
          يجري حالياً إعداد بوابة تسجيل دخول المعلمين ومساعدي السناتر مع حماية Brute-force المتقدمة. ستتاح البوابة متكاملة في المرحلة الرابعة.
        </p>

        <div class="stub-actions">
          <button type="button" class="btn-return" (click)="navigateHome()">
            <span class="material-symbols-outlined btn-icon">arrow_forward</span>
            العودة للرئيسية
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
      min-height: 100vh;
      align-items: center;
      justify-content: center;
      background-color: var(--bg-page, #f8fafc);
    }

    .login-stub-container {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 1.5rem;
      width: 100%;
    }

    .login-stub-card {
      background-color: var(--bg-surface, #ffffff);
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: var(--radius-lg, 16px);
      padding: 2.5rem;
      width: 100%;
      max-width: 32rem;
      text-align: center;
      box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.05);
      display: flex;
      flex-direction: column;
      align-items: center;
    }

    .brand-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      margin-block-end: 1.5rem;
    }

    .brand-cap {
      font-size: 1.75rem;
    }

    .brand-name {
      font-size: 1.25rem;
      font-weight: 800;
      color: var(--brand-primary, #1e3a8a);
    }

    .stub-icon-circle {
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

    .stub-icon-circle .material-symbols-outlined {
      font-size: 2.25rem;
    }

    .stub-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--text-primary, #0f172a);
      margin: 0;
      margin-block-end: 0.75rem;
      line-height: 1.5;
    }

    .stub-description {
      font-size: 0.9375rem;
      color: var(--text-secondary, #475569);
      margin: 0;
      margin-block-end: 2rem;
      line-height: 1.6;
    }

    .stub-actions {
      display: flex;
      width: 100%;
      justify-content: center;
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

    .btn-icon {
      font-size: 1.125rem;
    }
  `]
})
export class LoginStubComponent {
  constructor(private router: Router) {}

  navigateHome(): void {
    this.router.navigate(['/']);
  }
}
