import {
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  Output,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { SubscriptionInquiryRequest, SubscriptionInquiryResponse, PackageTier } from '../../../../core/models/public.models';

const EGYPTIAN_PHONE_REGEX = /^01[0125][0-9]{8}$/;

@Component({
  selector: 'app-subscription-inquiry-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    @if (isOpen) {
      <div class="modal-backdrop" (click)="onBackdropClick($event)">
        <div
          #dialogContainer
          class="modal-dialog"
          role="dialog"
          aria-modal="true"
          aria-labelledby="inquiry-title"
        >
          <div class="modal-header">
            <div>
              <h3 id="inquiry-title" class="modal-title">طلب اشتراك جديد في نظام تحضيرة</h3>
              <p class="modal-subtitle">اختر باقتك وسيقوم فريق الدعم بالتواصل معك لتفعيل النظام</p>
            </div>
            <button
              type="button"
              class="close-btn"
              (click)="onClose()"
              aria-label="إغلاق النافذة"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>

          @if (isSuccess) {
            <div class="success-container">
              <div class="success-icon-badge">
                <span class="material-symbols-outlined">check_circle</span>
              </div>
              <h4 class="success-title">تم استلام طلبكم بنجاح!</h4>
              <p class="success-message">{{ successMessage }}</p>
              <span class="success-timer">جاري إغلاق النافذة تلقائياً...</span>
            </div>
          } @else {
            <form [formGroup]="inquiryForm" (ngSubmit)="onSubmit()" class="inquiry-form">
              <div class="form-group">
                <label for="name" class="form-label">اسم المعلم أو السنتر <span class="required">*</span></label>
                <input
                  id="name"
                  type="text"
                  formControlName="name"
                  class="form-input"
                  placeholder="مثال: أ/ أحمد مصطفى أو سنتر الأوائل"
                  [class.invalid]="isFieldInvalid('name')"
                />
                @if (isFieldInvalid('name')) {
                  <span class="error-text">يرجى إدخال اسم المعلم أو السنتر (3 أحرف على الأقل).</span>
                }
              </div>

              <div class="form-group">
                <label for="phone" class="form-label">رقم هاتف الواتساب (11 رقم) <span class="required">*</span></label>
                <input
                  id="phone"
                  type="tel"
                  formControlName="phone"
                  class="form-input ltr-num"
                  placeholder="01012345678"
                  [class.invalid]="isFieldInvalid('phone')"
                />
                @if (isFieldInvalid('phone')) {
                  <span class="error-text">
                    رقم الهاتف غير صحيح (يجب أن يبدأ بـ 010 أو 011 أو 012 أو 015 ويحتوي على 11 رقم).
                  </span>
                }
              </div>

              <div class="form-row">
                <div class="form-group flex-1">
                  <label for="governorate" class="form-label">المحافظة <span class="required">*</span></label>
                  <select
                    id="governorate"
                    formControlName="governorate"
                    class="form-select"
                    [class.invalid]="isFieldInvalid('governorate')"
                  >
                    <option value="" disabled>اختر المحافظة...</option>
                    @for (gov of governorates; track gov) {
                      <option [value]="gov">{{ gov }}</option>
                    }
                  </select>
                  @if (isFieldInvalid('governorate')) {
                    <span class="error-text">يرجى اختيار المحافظة.</span>
                  }
                </div>

                <div class="form-group flex-1">
                  <label for="estimatedStudents" class="form-label">عدد الطلاب التقريبي <span class="required">*</span></label>
                  <select
                    id="estimatedStudents"
                    formControlName="estimatedStudents"
                    class="form-select"
                    [class.invalid]="isFieldInvalid('estimatedStudents')"
                  >
                    <option value="1-100">1 - 100 طالب</option>
                    <option value="100-300">100 - 300 طالب</option>
                    <option value="300-1000">300 - 1000 طالب</option>
                    <option value="+1000">أكثر من 1000 طالب</option>
                  </select>
                </div>
              </div>

              <div class="form-group">
                <label for="selectedPackage" class="form-label">الباقة المطلوبة <span class="required">*</span></label>
                <div class="package-chips">
                  <button
                    type="button"
                    class="chip-btn"
                    [class.selected]="inquiryForm.get('selectedPackage')?.value === 'Basic'"
                    (click)="selectPackage('Basic')"
                  >
                    الباقة الأساسية (199 ج.م)
                  </button>
                  <button
                    type="button"
                    class="chip-btn featured-chip"
                    [class.selected]="inquiryForm.get('selectedPackage')?.value === 'Pro'"
                    (click)="selectPackage('Pro')"
                  >
                    باقة المحترفين (349 ج.م)
                  </button>
                  <button
                    type="button"
                    class="chip-btn"
                    [class.selected]="inquiryForm.get('selectedPackage')?.value === 'Center'"
                    (click)="selectPackage('Center')"
                  >
                    باقة السنتر (599 ج.م)
                  </button>
                </div>
              </div>

              @if (apiError) {
                <div class="api-error-alert" role="alert">
                  <span class="material-symbols-outlined">error</span>
                  <span>{{ apiError }}</span>
                </div>
              }

              <div class="modal-footer">
                <button type="button" class="btn-cancel" (click)="onClose()">
                  إلغاء
                </button>
                <button
                  type="submit"
                  class="btn-submit"
                  [disabled]="inquiryForm.invalid || isSubmitting"
                >
                  @if (isSubmitting) {
                    <span class="spinner"></span>
                  }
                  إرسال طلب الاشتراك
                </button>
              </div>
            </form>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    :host {
      display: contents;
    }

    .modal-backdrop {
      position: fixed;
      inset: 0;
      z-index: 50;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: rgba(15, 23, 42, 0.65);
      backdrop-filter: blur(4px);
      padding: 1rem;
    }

    .modal-dialog {
      background-color: var(--bg-surface, #ffffff);
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: var(--radius-lg, 16px);
      width: 100%;
      max-width: 32rem;
      max-height: 90vh;
      overflow-y: auto;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.15);
      display: flex;
      flex-direction: column;
      animation: modal-enter 0.2s ease-out;
    }

    .modal-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      padding-block: 1.25rem;
      padding-inline: 1.5rem;
      border-block-end: 1px solid var(--border-subtle, #e2e8f0);
    }

    .modal-title {
      font-size: 1.125rem;
      font-weight: 700;
      color: var(--text-primary, #0f172a);
      margin: 0;
      margin-block-end: 0.25rem;
    }

    .modal-subtitle {
      font-size: 0.8125rem;
      color: var(--text-secondary, #475569);
      margin: 0;
    }

    .close-btn {
      background: transparent;
      border: none;
      cursor: pointer;
      color: var(--text-muted, #64748b);
      border-radius: 6px;
      padding: 4px;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .close-btn:focus-visible {
      outline: 3px solid var(--focus-ring, #3b82f6);
    }

    .inquiry-form {
      padding-block: 1.25rem;
      padding-inline: 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 0.375rem;
    }

    .form-row {
      display: flex;
      gap: 1rem;
    }

    .flex-1 {
      flex: 1;
    }

    .form-label {
      font-size: 0.8125rem;
      font-weight: 600;
      color: var(--text-primary, #0f172a);
    }

    .required {
      color: var(--color-danger, #ef4444);
    }

    .form-input,
    .form-select {
      width: 100%;
      padding-block: 0.625rem;
      padding-inline: 0.75rem;
      font-size: 0.875rem;
      border: 1px solid var(--border-strong, #cbd5e1);
      border-radius: var(--radius-md, 8px);
      background-color: var(--bg-surface, #ffffff);
      color: var(--text-primary, #0f172a);
      outline: none;
      box-sizing: border-box;
      transition: border-color 0.15s ease;
    }

    .form-input:focus-visible,
    .form-select:focus-visible {
      border-color: var(--brand-accent, #3b82f6);
      outline: 3px solid var(--focus-ring, #3b82f6);
      outline-offset: 1px;
    }

    .form-input.invalid,
    .form-select.invalid {
      border-color: var(--color-danger, #ef4444);
    }

    .error-text {
      font-size: 0.75rem;
      color: var(--color-danger, #ef4444);
      line-height: 1.4;
    }

    .package-chips {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 0.5rem;
    }

    .chip-btn {
      padding-block: 0.5rem;
      padding-inline: 0.25rem;
      font-size: 0.75rem;
      font-weight: 600;
      border-radius: var(--radius-md, 8px);
      border: 1px solid var(--border-strong, #cbd5e1);
      background-color: var(--bg-surface-elevated, #f8fafc);
      color: var(--text-primary, #0f172a);
      cursor: pointer;
      text-align: center;
      transition: all 0.15s ease;
    }

    .chip-btn.selected {
      border-color: var(--brand-accent, #3b82f6);
      background-color: var(--brand-accent-bg, #eff6ff);
      color: var(--brand-accent, #3b82f6);
      font-weight: 700;
    }

    .api-error-alert {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.625rem;
      border-radius: 8px;
      background-color: #fef2f2;
      border: 1px solid #fecaca;
      color: var(--color-danger, #ef4444);
      font-size: 0.8125rem;
    }

    .modal-footer {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 0.75rem;
      margin-block-start: 0.5rem;
      padding-block-start: 1rem;
      border-block-start: 1px solid var(--border-subtle, #e2e8f0);
    }

    .btn-cancel,
    .btn-submit {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding-block: 0.625rem;
      padding-inline: 1.25rem;
      font-size: 0.875rem;
      font-weight: 600;
      border-radius: var(--radius-md, 8px);
      cursor: pointer;
      outline: none;
      transition: background-color 0.15s ease;
    }

    .btn-cancel:focus-visible,
    .btn-submit:focus-visible {
      outline: 3px solid var(--focus-ring, #3b82f6);
      outline-offset: 2px;
    }

    .btn-cancel {
      background-color: transparent;
      border: 1px solid var(--border-strong, #cbd5e1);
      color: var(--text-primary, #0f172a);
    }

    .btn-submit {
      background-color: var(--brand-primary, #1e3a8a);
      color: #ffffff;
      border: 1px solid transparent;
    }

    .btn-submit:hover:not(:disabled) {
      background-color: var(--brand-primary-hover, #172554);
    }

    .btn-submit:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .spinner {
      width: 1rem;
      height: 1rem;
      border: 2px solid #ffffff;
      border-block-start-color: transparent;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }

    .success-container {
      padding: 3rem 1.5rem;
      text-align: center;
      display: flex;
      flex-direction: column;
      align-items: center;
    }

    .success-icon-badge {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 4rem;
      height: 4rem;
      border-radius: 50%;
      background-color: #ecfdf5;
      color: #10b981;
      margin-block-end: 1rem;
    }

    .success-icon-badge .material-symbols-outlined {
      font-size: 2.5rem;
    }

    .success-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--text-primary, #0f172a);
      margin: 0;
      margin-block-end: 0.5rem;
    }

    .success-message {
      font-size: 0.9375rem;
      color: var(--text-secondary, #475569);
      margin: 0;
      margin-block-end: 1.5rem;
      max-width: 24rem;
      line-height: 1.6;
    }

    .success-timer {
      font-size: 0.8125rem;
      color: var(--text-muted, #64748b);
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    @keyframes modal-enter {
      from { opacity: 0; transform: scale(0.95); }
      to { opacity: 1; transform: scale(1); }
    }
  `]
})
export class SubscriptionInquiryModalComponent {
  @Input() isOpen: boolean = false;
  @Input() initialPackage: PackageTier = 'Pro';

  @Output() closed = new EventEmitter<void>();
  @Output() inquirySubmitted = new EventEmitter<SubscriptionInquiryResponse>();

  @ViewChild('dialogContainer') dialogContainer?: ElementRef<HTMLElement>;

  inquiryForm: FormGroup;
  isSubmitting: boolean = false;
  isSuccess: boolean = false;
  successMessage: string = '';
  apiError: string = '';

  readonly governorates: string[] = [
    'القاهرة', 'الجيزة', 'الإسكندرية', 'الدقهلية', 'الغربية', 'المنوفية',
    'الشرقية', 'القليوبية', 'البحيرة', 'كفر الشيخ', 'دمياط', 'بورسعيد',
    'الإسماعيلية', 'السويس', 'الفيوم', 'بني سويف', 'المنيا', 'أسيوط',
    'سوهاج', 'قنا', 'الأقصر', 'أسوان', 'البحر الأحمر', 'الوادي الجديد',
    'مطروح', 'شمال سيناء', 'جنوب سيناء'
  ];

  constructor(private fb: FormBuilder, private http: HttpClient) {
    this.inquiryForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      phone: ['', [Validators.required, Validators.pattern(EGYPTIAN_PHONE_REGEX)]],
      governorate: ['', [Validators.required]],
      estimatedStudents: ['100-300', [Validators.required]],
      selectedPackage: [this.initialPackage, [Validators.required]]
    });
  }

  @HostListener('window:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (!this.isOpen) return;

    if (event.key === 'Escape') {
      event.preventDefault();
      this.onClose();
      return;
    }

    if (event.key === 'Tab') {
      this.trapFocus(event);
    }
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.inquiryForm.get(fieldName);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  selectPackage(pkg: PackageTier): void {
    this.inquiryForm.patchValue({ selectedPackage: pkg });
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-backdrop')) {
      this.onClose();
    }
  }

  onSubmit(): void {
    if (this.inquiryForm.invalid || this.isSubmitting) {
      this.inquiryForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.apiError = '';

    const payload: SubscriptionInquiryRequest = this.inquiryForm.value;

    this.http.post<SubscriptionInquiryResponse>('/api/v1/public/inquiries', payload).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        this.isSuccess = true;
        this.successMessage = res.message || 'تم استلام طلبكم بنجاح وسيتواصل معكم فريق الدعم خلال ساعتين.';
        this.inquirySubmitted.emit(res);

        setTimeout(() => {
          this.onClose();
        }, 2000);
      },
      error: (err) => {
        this.isSubmitting = false;
        if (err.error?.errors?.phone) {
          this.apiError = err.error.errors.phone[0];
        } else if (err.error?.detail) {
          this.apiError = err.error.detail;
        } else {
          this.apiError = 'حدث خطأ أثناء إرسال الطلب. يرجى التأكد من البيانات والمحاولة مجدداً.';
        }
      }
    });
  }

  onClose(): void {
    this.isOpen = false;
    this.isSuccess = false;
    this.isSubmitting = false;
    this.apiError = '';
    this.inquiryForm.reset({
      name: '',
      phone: '',
      governorate: '',
      estimatedStudents: '100-300',
      selectedPackage: this.initialPackage
    });
    this.closed.emit();
  }

  private trapFocus(event: KeyboardEvent): void {
    if (!this.dialogContainer?.nativeElement) return;

    const focusables = this.dialogContainer.nativeElement.querySelectorAll<HTMLElement>(
      'button, input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    if (!focusables || focusables.length === 0) return;

    const first = focusables[0];
    const last = focusables[focusables.length - 1];

    if (event.shiftKey && document.activeElement === first) {
      last.focus();
      event.preventDefault();
    } else if (!event.shiftKey && document.activeElement === last) {
      first.focus();
      event.preventDefault();
    }
  }
}
