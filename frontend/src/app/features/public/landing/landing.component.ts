import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LocalizationService } from '../../../core/services/localization.service';
import { ThemeService } from '../../../core/services/theme.service';
import { AuthStore } from '../../../core/stores/auth.store';
import { SubscriptionInquiryModalComponent } from './subscription-inquiry-modal/subscription-inquiry-modal.component';
import { PackageTier } from '../../../core/models/public.models';

interface FaqItem {
  questionKey: string;
  answerKey: string;
  isOpen: boolean;
}

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, RouterModule, SubscriptionInquiryModalComponent],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.css']
})
export class LandingComponent {
  readonly localization = inject(LocalizationService);
  readonly theme = inject(ThemeService);
  readonly authStore = inject(AuthStore);

  readonly isAuthenticated = computed(() => this.authStore.isAuthenticated());

  isModalOpen: boolean = false;
  selectedPackage: PackageTier = 'Pro';

  faqItems: FaqItem[] = [
    {
      questionKey: 'هل أستطيع إنشاء الحساب بنفسي؟',
      answerKey: 'الحسابات ينشئها مالك المنصة فقط بعد الاتفاق لضمان الأمان وتخصيص الباقة. تواصل معنا لطلب الاشتراك واستلام بيانات الدخول.',
      isOpen: false
    },
    {
      questionKey: 'متى يُحتسب الطالب غائباً؟',
      answerKey: 'بعد الموعد النهائي الفعلي للحصة وانتهاء مهلة السماح المحددة (مثل 20 دقيقة أو 24 ساعة). الحصص الملغاة والإجازات مستبعدة تماماً.',
      isOpen: false
    },
    {
      questionKey: 'هل إرسال واتساب يؤثر على تسجيل الحضور في حالة انقطاع النت؟',
      answerKey: 'لا، الحضور يُحفظ أولاً في المتصفح والذاكرة المحلية بموثوقية تامة، ورسائل واتساب تنتظر في طابور الإرسال حتى عودة الاتصال.',
      isOpen: false
    },
    {
      questionKey: 'ماذا يحدث بعد انتهاء فترة الاشتراك؟',
      answerKey: 'تتعطل وظائف التسجيل مؤقتاً مع الاحتفاظ الكامل بجميع سجلات الطلاب والحضور والمدفوعات دون أي حذف.',
      isOpen: false
    }
  ];

  openSubscribeModal(pkg: PackageTier = 'Pro'): void {
    this.selectedPackage = pkg;
    this.isModalOpen = true;
  }

  closeSubscribeModal(): void {
    this.isModalOpen = false;
  }

  toggleFaq(index: number): void {
    this.faqItems[index].isOpen = !this.faqItems[index].isOpen;
  }

  toggleLanguage(): void {
    this.localization.toggleLanguage();
  }

  toggleTheme(): void {
    this.theme.toggleTheme();
  }
}
