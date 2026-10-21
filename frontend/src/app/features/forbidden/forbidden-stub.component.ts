import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthStore } from '../../core/stores/auth.store';
import { TranslatePipe } from '../../core/pipes/translate.pipe';

@Component({
  selector: 'app-forbidden-stub',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  template: `
    <div class="forbidden-container">
      <div class="forbidden-card">
        <div class="icon-wrap">
          <span class="lock-icon" aria-hidden="true">🛡️</span>
          <span class="code-badge ltr-num font-mono">403</span>
        </div>
        <h1 class="forbidden-title">{{ 'FORBIDDEN_PAGE.TITLE' | translate }}</h1>
        <p class="forbidden-subtitle">
          {{ 'FORBIDDEN_PAGE.SUBTEXT' | translate }}
        </p>
        <div class="action-buttons">
          <button type="button" class="btn btn-primary" (click)="returnToDashboard()">
            {{ 'FORBIDDEN_PAGE.RETURN_BTN' | translate }}
          </button>
          <button type="button" class="btn btn-secondary" (click)="logout()">
            تسجيل الخروج
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .forbidden-container {
      min-block-size: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding-block: 2rem;
      padding-inline: 1rem;
      background: var(--surface-ground, #F8FAFC);
    }
    .forbidden-card {
      inline-size: 100%;
      max-inline-size: 32rem;
      background: var(--surface-card, #FFFFFF);
      border-radius: 1rem;
      padding-block: 2.5rem;
      padding-inline: 2rem;
      text-align: center;
      box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.05), 0 8px 10px -6px rgba(0, 0, 0, 0.01);
      border: 1px solid var(--surface-border, #E2E8F0);
    }
    .icon-wrap {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.75rem;
      margin-block-end: 1.5rem;
    }
    .lock-icon {
      font-size: 2.5rem;
    }
    .code-badge {
      display: inline-flex;
      align-items: center;
      padding-block: 0.25rem;
      padding-inline: 0.75rem;
      background: #FEE2E2;
      color: #991B1B;
      font-weight: 700;
      font-size: 1.25rem;
      border-radius: 9999px;
    }
    .forbidden-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--text-color, #0F172A);
      margin-block-end: 0.75rem;
      line-height: 1.5;
    }
    .forbidden-subtitle {
      font-size: 0.95rem;
      color: var(--text-color-secondary, #64748B);
      margin-block-end: 2rem;
      line-height: 1.6;
    }
    .action-buttons {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }
    .btn {
      padding-block: 0.75rem;
      padding-inline: 1.5rem;
      border-radius: 0.5rem;
      font-weight: 600;
      font-size: 0.95rem;
      cursor: pointer;
      border: 1px solid transparent;
      transition: background-color 0.2s, color 0.2s;
    }
    .btn-primary {
      background: #0284C7;
      color: #FFFFFF;
    }
    .btn-primary:hover {
      background: #0369A1;
    }
    .btn-secondary {
      background: transparent;
      color: #64748B;
      border-color: #CBD5E1;
    }
    .btn-secondary:hover {
      background: #F1F5F9;
      color: #0F172A;
    }
  `]
})
export class ForbiddenStubComponent {
  private router = inject(Router);
  private authStore = inject(AuthStore);

  returnToDashboard(): void {
    this.router.navigate(['/workspace/dashboard']);
  }

  logout(): void {
    this.authStore.logout();
    this.router.navigate(['/login']);
  }
}
