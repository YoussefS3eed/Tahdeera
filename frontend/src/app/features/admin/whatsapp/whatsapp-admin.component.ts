import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminStore } from '../../../core/stores/admin.store';
import { WhatsAppProviderConfig } from '../../../core/models/admin.models';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

@Component({
  selector: 'app-whatsapp-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  template: `
    <div class="whatsapp-admin-container">
      <header class="page-header">
        <h2 class="page-title">{{ 'ADMIN_WHATSAPP.PAGE_TITLE' | translate }}</h2>
        <p class="page-subtitle">{{ 'ADMIN_WHATSAPP.SUBTITLE' | translate }}</p>
      </header>

      <!-- Connection Status & Ping Card -->
      <section class="section-card highlight-card">
        <div class="ping-row">
          <div class="ping-info">
            <span class="ping-title">{{ 'ADMIN_WHATSAPP.CREDENTIALS_CARD' | translate }}</span>
            <div class="status-tags">
              <span class="badge" [class.badge-success]="pingSuccess" [class.badge-neutral]="!pingSuccess && !pingLoading">
                {{ pingSuccess ? 'متصل بنجاح (Connected)' : 'جاهز للفحص' }}
              </span>
              <span class="badge badge-info">Phone: +201272785618</span>
              <span class="badge badge-success">Quality: High (Green)</span>
              <span class="badge badge-neutral">Tier 2: 10K msgs/day</span>
            </div>
          </div>
          <button
            type="button"
            class="btn btn-primary"
            [disabled]="pingLoading"
            (click)="testPing()"
          >
            {{ pingLoading ? 'جارٍ الاتصال (300ms)...' : '⚡ اختبار الاتصال بمزود الخدمة' }}
          </button>
        </div>
      </section>

      <!-- 4 Delivery Counters (CONF-04: Non-PII) -->
      @if (adminStore.whatsappStats(); as stats) {
        <div class="stats-grid">
          <div class="stat-card">
            <span class="stat-title">{{ 'ADMIN_WHATSAPP.OUTBOX_TOTAL' | translate }}</span>
            <span class="stat-value ltr-num font-mono">{{ stats.totalSent }}</span>
          </div>
          <div class="stat-card">
            <span class="stat-title">{{ 'ADMIN_WHATSAPP.DELIVERED_TOTAL' | translate }}</span>
            <span class="stat-value text-emerald ltr-num font-mono">{{ stats.delivered }}</span>
          </div>
          <div class="stat-card">
            <span class="stat-title">تمت القراءة (Read)</span>
            <span class="stat-value text-sky ltr-num font-mono">{{ stats.read }}</span>
          </div>
          <div class="stat-card">
            <span class="stat-title">{{ 'ADMIN_WHATSAPP.PERMANENT_FAILURES' | translate }}</span>
            <span class="stat-value text-danger ltr-num font-mono">{{ stats.failed }}</span>
          </div>
        </div>
      }

      <div class="content-grid">
        <!-- Credentials Form with Masking -->
        <section class="section-card">
          <h3 class="section-title">{{ 'ADMIN_WHATSAPP.CREDENTIALS_CARD' | translate }}</h3>
          <form (ngSubmit)="saveConfig()">
            <div class="form-group">
              <label>{{ 'ADMIN_WHATSAPP.FIELD_PHONE_ID' | translate }}</label>
              <input type="text" [(ngModel)]="config.phoneNumberId" name="phoneNumberId" class="font-mono ltr-num" />
            </div>
            <div class="form-group">
              <label>{{ 'ADMIN_WHATSAPP.FIELD_WABA_ID' | translate }}</label>
              <input type="text" [(ngModel)]="config.wabaId" name="wabaId" class="font-mono ltr-num" />
            </div>
            <div class="form-group">
              <div class="label-row">
                <label>{{ 'ADMIN_WHATSAPP.FIELD_TOKEN' | translate }}</label>
                <button type="button" class="btn-toggle-mask" (click)="showToken = !showToken">
                  {{ showToken ? 'إخفاء' : 'إظهار' }}
                </button>
              </div>
              <input
                [type]="showToken ? 'text' : 'password'"
                [(ngModel)]="config.accessToken"
                name="accessToken"
                class="font-mono"
              />
            </div>
            <div class="form-group">
              <div class="label-row">
                <label>{{ 'ADMIN_WHATSAPP.FIELD_SECRET' | translate }}</label>
                <button type="button" class="btn-toggle-mask" (click)="showSecret = !showSecret">
                  {{ showSecret ? 'إخفاء' : 'إظهار' }}
                </button>
              </div>
              <input
                [type]="showSecret ? 'text' : 'password'"
                [(ngModel)]="config.appSecret"
                name="appSecret"
                class="font-mono"
              />
            </div>
            <div class="form-group">
              <label>Webhook Verify Token</label>
              <input type="text" [(ngModel)]="config.webhookVerifyToken" name="webhookVerifyToken" class="font-mono" />
            </div>

            <button type="submit" class="btn btn-primary">{{ 'ADMIN_WHATSAPP.SAVE_CREDENTIALS_BTN' | translate }}</button>
          </form>
        </section>

        <!-- Retry Policy Timeline (DEC-07) -->
        <section class="section-card">
          <h3 class="section-title">{{ 'ADMIN_WHATSAPP.RETRY_POLICY_TITLE' | translate }}</h3>
          <p class="section-desc">
            {{ 'ADMIN_WHATSAPP.RETRY_EXPLANATION' | translate }}
          </p>
          <div class="retry-timeline">
            <div class="retry-step">
              <span class="step-num ltr-num font-mono">1</span>
              <div class="step-body">
                <strong>المحاولة الأولى: فوري</strong>
                <span>عند وقوع الحدث مباشرة (تسجيل الحضور / المصروفات)</span>
              </div>
            </div>
            <div class="retry-step">
              <span class="step-num ltr-num font-mono">2</span>
              <div class="step-body">
                <strong>المحاولة الثانية: بعد دقيقة واحدة (1m)</strong>
                <span>في حال بطء الشبكة أو انقطاع الاتصال المؤقت</span>
              </div>
            </div>
            <div class="retry-step">
              <span class="step-num ltr-num font-mono">3</span>
              <div class="step-body">
                <strong>المحاولة الثالثة: بعد 5 دقائق (5m)</strong>
                <span>إعادة جدولة عبر طابور الانتظار</span>
              </div>
            </div>
            <div class="retry-step">
              <span class="step-num ltr-num font-mono">4</span>
              <div class="step-body">
                <strong>المحاولة الرابعة: بعد 15 دقيقة (15m)</strong>
                <span>التحقق من حالة رقم المستلم</span>
              </div>
            </div>
            <div class="retry-step">
              <span class="step-num ltr-num font-mono">5</span>
              <div class="step-body">
                <strong>المحاولة الخامسة: بعد 60 دقيقة (60m)</strong>
                <span>المحاولة الأخيرة قبل الإحالة لطابور الرسائل الميتة (Dead-Letter)</span>
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>
  `,
  styles: [`
    .whatsapp-admin-container {
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
    .highlight-card {
      border-color: #0284C7;
      background: linear-gradient(135deg, #0B2244 0%, #081A35 100%);
    }
    .ping-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1.5rem;
      flex-wrap: wrap;
    }
    .ping-info {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }
    .ping-title {
      font-size: 1.1rem;
      font-weight: 600;
      color: #F8FAFC;
    }
    .status-tags {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }
    .badge {
      display: inline-block;
      padding-block: 0.25rem;
      padding-inline: 0.65rem;
      border-radius: 9999px;
      font-size: 0.8rem;
      font-weight: 600;
    }
    .badge-success { background: rgba(16, 185, 129, 0.2); color: #34D399; }
    .badge-info { background: rgba(56, 189, 248, 0.2); color: #38BDF8; }
    .badge-neutral { background: #1E293B; color: #94A3B8; }
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1.25rem;
    }
    .stat-card {
      background: #0B2244;
      border: 1px solid #1E3A5F;
      border-radius: 0.75rem;
      padding-block: 1.25rem;
      padding-inline: 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
    }
    .stat-title { font-size: 0.85rem; color: #94A3B8; }
    .stat-value { font-size: 1.75rem; font-weight: 700; color: #F8FAFC; }
    .text-emerald { color: #10B981; }
    .text-sky { color: #38BDF8; }
    .text-danger { color: #EF4444; }
    .content-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1.5rem;
    }
    @media (max-width: 960px) {
      .content-grid { grid-template-columns: 1fr; }
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
    .label-row { display: flex; justify-content: space-between; align-items: center; }
    .btn-toggle-mask {
      background: none;
      border: none;
      color: #38BDF8;
      font-size: 0.8rem;
      cursor: pointer;
    }
    .form-group input {
      padding-block: 0.6rem;
      padding-inline: 0.85rem;
      background: #081A35;
      border: 1px solid #1E3A5F;
      border-radius: 0.375rem;
      color: #F8FAFC;
      font-size: 0.9rem;
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
    .retry-timeline {
      display: flex;
      flex-direction: column;
      gap: 0.85rem;
      margin-block-start: 0.5rem;
    }
    .retry-step {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
    }
    .step-num {
      display: flex;
      align-items: center;
      justify-content: center;
      inline-size: 1.75rem;
      block-size: 1.75rem;
      border-radius: 9999px;
      background: #1E3A5F;
      color: #38BDF8;
      font-weight: 700;
      font-size: 0.85rem;
      flex-shrink: 0;
    }
    .step-body {
      display: flex;
      flex-direction: column;
      font-size: 0.85rem;
    }
    .step-body strong { color: #F8FAFC; margin-block-end: 0.15rem; }
    .step-body span { color: #94A3B8; }
  `]
})
export class WhatsAppAdminComponent implements OnInit {
  readonly adminStore = inject(AdminStore);

  showToken = false;
  showSecret = false;
  pingLoading = false;
  pingSuccess = false;

  config: WhatsAppProviderConfig = {
    phoneNumberId: 'phone_10928374',
    wabaId: 'waba_992837461',
    accessToken: 'EAAGxxxxx_mock_meta_token',
    appSecret: 'secret_abc_123',
    webhookVerifyToken: 'tahdeera_webhook_verify_2026',
    status: 'Connected',
    qualityRating: 'Green'
  };

  ngOnInit(): void {
    this.adminStore.loadWhatsAppConfig().subscribe(res => {
      if (res?.config) {
        this.config = { ...res.config };
      }
    });
  }

  testPing(): void {
    this.pingLoading = true;
    this.adminStore.testWhatsAppPing().subscribe({
      next: () => {
        this.pingLoading = false;
        this.pingSuccess = true;
      },
      error: () => {
        this.pingLoading = false;
        this.pingSuccess = false;
      }
    });
  }

  saveConfig(): void {
    // Saves central gateway credentials
  }
}
