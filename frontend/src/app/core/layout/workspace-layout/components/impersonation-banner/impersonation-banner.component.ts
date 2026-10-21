import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminImpersonationStore } from '../../../../stores/admin-impersonation.store';
import { TranslatePipe } from '../../../../pipes/translate.pipe';

@Component({
  selector: 'app-impersonation-banner',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  template: `
    @if (impersonationStore.isImpersonating()) {
      <div class="impersonation-banner" role="alert">
        <div class="banner-content">
          <div class="banner-text">
            <span class="banner-icon" aria-hidden="true">⚠️</span>
            <span class="banner-label">
              {{ 'ADMIN_SHELL.IMPERSONATING_NOTICE' | translate }}
              <strong>{{ impersonationStore.targetWorkspace()?.name }}</strong>
            </span>
          </div>
          <button
            type="button"
            class="exit-btn"
            (click)="impersonationStore.exitImpersonation()"
          >
            {{ 'ADMIN_SHELL.END_IMPERSONATION' | translate }}
          </button>
        </div>
      </div>
    }
  `,
  styles: [`
    .impersonation-banner {
      position: sticky;
      inset-block-start: 0;
      z-index: 100;
      background: #FEF3C7;
      border-block-end: 2px solid #F59E0B;
      color: #92400E;
      padding-block: 0.65rem;
      padding-inline: 1.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
    }
    .banner-content {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      max-inline-size: 1440px;
      margin-inline: auto;
    }
    .banner-text {
      display: flex;
      align-items: center;
      gap: 0.65rem;
      font-size: 0.95rem;
    }
    .banner-icon {
      font-size: 1.25rem;
    }
    .exit-btn {
      padding-block: 0.4rem;
      padding-inline: 0.9rem;
      background: #B45309;
      color: #FFFFFF;
      font-weight: 600;
      font-size: 0.85rem;
      border: none;
      border-radius: 0.375rem;
      cursor: pointer;
      white-space: nowrap;
      transition: background-color 0.2s;
    }
    .exit-btn:hover {
      background: #92400E;
    }
    @media (max-width: 640px) {
      .banner-content {
        flex-direction: column;
        align-items: flex-start;
        gap: 0.5rem;
      }
      .exit-btn {
        inline-size: 100%;
        text-align: center;
      }
    }
  `]
})
export class ImpersonationBannerComponent {
  readonly impersonationStore = inject(AdminImpersonationStore);
}
