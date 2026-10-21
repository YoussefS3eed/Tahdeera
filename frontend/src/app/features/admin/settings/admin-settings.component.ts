import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminStore } from '../../../core/stores/admin.store';
import { PlatformGlobalSettings } from '../../../core/models/admin.models';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  template: `
    <div class="settings-container">
      <header class="page-header">
        <h2 class="page-title">{{ 'ADMIN_SETTINGS.PAGE_TITLE' | translate }}</h2>
        <p class="page-subtitle">{{ 'ADMIN_SETTINGS.SUBTITLE' | translate }}</p>
      </header>

      <div class="content-grid">
        <!-- Settings Form -->
        <section class="section-card">
          <h3 class="section-title">الهوية وأرقام الدعم الفني</h3>
          <form (ngSubmit)="saveSettings()">
            <div class="form-group">
              <label>{{ 'ADMIN_SETTINGS.PLATFORM_NAME' | translate }} *</label>
              <input type="text" [(ngModel)]="settings.platformName" name="platformName" required />
            </div>
            <div class="form-group">
              <label>{{ 'ADMIN_SETTINGS.PLATFORM_TAGLINE' | translate }} *</label>
              <input type="text" [(ngModel)]="settings.tagline" name="tagline" required />
            </div>

            <div class="form-row">
              <div class="form-group">
                <label>{{ 'ADMIN_SETTINGS.PRIMARY_PHONE' | translate }} *</label>
                <input
                  type="text"
                  [(ngModel)]="settings.primarySupportHotline"
                  name="primarySupportHotline"
                  required
                  pattern="^01[0125][0-9]{8}$"
                  class="font-mono ltr-num"
                />
              </div>
              <div class="form-group">
                <label>{{ 'ADMIN_SETTINGS.SECONDARY_PHONE' | translate }} *</label>
                <input
                  type="text"
                  [(ngModel)]="settings.secondarySupportHotline"
                  name="secondarySupportHotline"
                  required
                  pattern="^01[0125][0-9]{8}$"
                  class="font-mono ltr-num"
                />
              </div>
            </div>

            <div class="form-row">
              <div class="form-group">
                <label>البريد الإلكتروني للدعم الفني *</label>
                <input type="email" [(ngModel)]="settings.supportEmail" name="supportEmail" required class="font-mono" />
              </div>
              <div class="form-group">
                <label>سياسة الاحتفاظ بسجلات العمليات (أيام) *</label>
                <input type="number" [(ngModel)]="settings.auditLogRetentionDays" name="auditLogRetentionDays" required min="30" class="ltr-num font-mono" />
              </div>
            </div>

            <div class="form-group">
              <label class="checkbox-row">
                <input type="checkbox" [(ngModel)]="settings.allowNewRegistrations" name="allowNewRegistrations" />
                <span>إتاحة طلبات التسجيل العامة عبر الموقع الرئيسي</span>
              </label>
            </div>

            <button type="submit" class="btn btn-primary">{{ 'ADMIN_SETTINGS.SAVE_SETTINGS_BTN' | translate }}</button>
          </form>
        </section>

        <!-- Lockout Screen Propagation Preview -->
        <section class="section-card preview-card">
          <h3 class="section-title">{{ 'ADMIN_SETTINGS.HOTLINES_TITLE' | translate }}</h3>
          <p class="section-desc">
            {{ 'ADMIN_SETTINGS.HOTLINES_DESC' | translate }}
          </p>

          <div class="mock-lockout-box">
            <div class="lockout-badge">🔒 الحساب موقوف إدارياً أو منتهي الاشتراك</div>
            <p class="lockout-text">
              لتجديد الاشتراك وتفعيل الحساب فورياً، يرجى التواصل مع إدارة منصة {{ settings.platformName }}:
            </p>
            <div class="hotline-links">
              <a [href]="'https://wa.me/2' + settings.primarySupportHotline" target="_blank" class="hotline-item">
                <span class="hl-icon">💬</span>
                <div>
                  <span class="hl-title">الدعم الأساسي (واتساب):</span>
                  <span class="ltr-num font-mono hl-num">{{ settings.primarySupportHotline }}</span>
                </div>
              </a>
              <a [href]="'tel:' + settings.secondarySupportHotline" class="hotline-item">
                <span class="hl-icon">📞</span>
                <div>
                  <span class="hl-title">الخط الساخن الثانوي:</span>
                  <span class="ltr-num font-mono hl-num">{{ settings.secondarySupportHotline }}</span>
                </div>
              </a>
            </div>
          </div>
        </section>
      </div>
    </div>
  `,
  styles: [`
    .settings-container {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }
    .page-header {
      margin-block-end: 0.5rem;
    }
    .page-title {
      font-size: 1.5rem;
      font-weight: 700;
      color: #F8FAFC;
      margin-block-end: 0.25rem;
    }
    .page-subtitle {
      font-size: 0.95rem;
      color: #94A3B8;
    }
    .content-grid {
      display: grid;
      grid-template-columns: 3fr 2fr;
      gap: 1.5rem;
    }
    @media (max-width: 960px) {
      .content-grid { grid-template-columns: 1fr; }
    }
    .section-card {
      background: #0B2244;
      border: 1px solid #1E3A5F;
      border-radius: 0.75rem;
      padding-block: 1.5rem;
      padding-inline: 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }
    .preview-card {
      background: #081A35;
      border-color: #0284C7;
    }
    .section-title { font-size: 1.15rem; font-weight: 600; color: #F8FAFC; margin: 0; }
    .section-desc { font-size: 0.85rem; color: #94A3B8; margin: 0; line-height: 1.5; }
    .form-group {
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
      margin-block-end: 1rem;
    }
    .form-group label { font-size: 0.85rem; color: #94A3B8; }
    .form-group input {
      padding-block: 0.6rem;
      padding-inline: 0.85rem;
      background: #081A35;
      border: 1px solid #1E3A5F;
      border-radius: 0.375rem;
      color: #F8FAFC;
      font-size: 0.9rem;
    }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .checkbox-row {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      color: #F8FAFC;
      font-size: 0.9rem;
      cursor: pointer;
    }
    .btn {
      padding-block: 0.65rem;
      padding-inline: 1.25rem;
      border-radius: 0.5rem;
      font-weight: 600;
      font-size: 0.9rem;
      cursor: pointer;
      border: none;
    }
    .btn-primary { background: #0284C7; color: #FFFFFF; }
    .btn-primary:hover { background: #0369A1; }
    .mock-lockout-box {
      background: #0B2244;
      border: 1px solid #F59E0B;
      border-radius: 0.5rem;
      padding: 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }
    .lockout-badge {
      font-size: 0.85rem;
      font-weight: 700;
      color: #FBBF24;
    }
    .lockout-text {
      font-size: 0.85rem;
      color: #CBD5E1;
      margin: 0;
      line-height: 1.5;
    }
    .hotline-links {
      display: flex;
      flex-direction: column;
      gap: 0.65rem;
      margin-block-start: 0.5rem;
    }
    .hotline-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      background: #061329;
      border: 1px solid #1E3A5F;
      padding: 0.65rem 0.85rem;
      border-radius: 0.375rem;
      text-decoration: none;
      color: #F8FAFC;
    }
    .hl-icon { font-size: 1.25rem; }
    .hl-title { font-size: 0.8rem; color: #94A3B8; display: block; }
    .hl-num { font-size: 1rem; font-weight: 700; color: #38BDF8; }
  `]
})
export class AdminSettingsComponent implements OnInit {
  readonly adminStore = inject(AdminStore);

  settings: PlatformGlobalSettings = {
    platformName: 'تحضيرة',
    tagline: 'المنصة الذكية لإدارة السناتر التعليمية وحضور الطلاب بالذكاء الاصطناعي',
    primarySupportHotline: '01272785618',
    secondarySupportHotline: '01019482687',
    supportEmail: 'support@tahdeera.app',
    allowNewRegistrations: true,
    maintenanceMode: false,
    auditLogRetentionDays: 365
  };

  ngOnInit(): void {
    this.adminStore.loadSettings().subscribe(res => {
      if (res) {
        this.settings = { ...res };
      }
    });
  }

  saveSettings(): void {
    this.adminStore.updateSettings(this.settings).subscribe();
  }
}
