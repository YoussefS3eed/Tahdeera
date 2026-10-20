/**
 * Login Route Stub Component
 * Milestone F1 Phase 7: Temporary destination for unauthenticated route guard redirection
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/pipes/translate.pipe';

@Component({
  selector: 'app-login-stub',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe],
  template: `
    <div class="stub-container">
      <div class="stub-card">
        <div class="stub-icon auth-icon">🔐</div>
        <h1 class="stub-title">{{ 'stubs.auth_required_title' | translate }}</h1>
        <p class="stub-desc">{{ 'stubs.auth_required_desc' | translate }}</p>

        <div class="stub-meta">
          <span class="badge badge-warning">Milestone F1 Stub</span>
        </div>

        <div class="stub-actions">
          <a routerLink="/harness" class="btn btn-primary">
            {{ 'stubs.return_to_harness' | translate }}
          </a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .stub-container {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 80vh;
      padding: 1.5rem;
    }

    .stub-card {
      background: var(--bg-card, #ffffff);
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: var(--radius-lg, 12px);
      padding: 2.5rem;
      max-width: 480px;
      width: 100%;
      text-align: center;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
    }

    .stub-icon {
      font-size: 3rem;
      margin-bottom: 1rem;
    }

    .stub-title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--text-primary, #0f172a);
      margin-bottom: 0.75rem;
    }

    .stub-desc {
      font-size: 0.95rem;
      color: var(--text-secondary, #475569);
      line-height: 1.5;
      margin-bottom: 1.5rem;
    }

    .stub-meta {
      margin-bottom: 1.5rem;
    }

    .badge-warning {
      background-color: var(--badge-late-bg, #fef3c7);
      color: var(--badge-late-text, #92400e);
      padding: 0.25rem 0.75rem;
      border-radius: 9999px;
      font-size: 0.8rem;
      font-weight: 600;
    }

    .btn-primary {
      display: inline-block;
      background-color: var(--primary, #1565c0);
      color: #ffffff;
      padding: 0.75rem 1.5rem;
      border-radius: var(--radius-md, 8px);
      text-decoration: none;
      font-weight: 600;
      transition: background-color 0.2s;
    }

    .btn-primary:hover {
      background-color: var(--primary-hover, #0d47a1);
    }
  `]
})
export class LoginStubComponent {}
