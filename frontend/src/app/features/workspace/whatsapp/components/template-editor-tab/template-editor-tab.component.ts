import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WhatsAppStore } from '../../../../../core/stores/whatsapp.store';
import { WhatsAppMessageType } from '../../../../../core/models/whatsapp.models';

@Component({
  selector: 'app-template-editor-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './template-editor-tab.component.html',
  styleUrls: ['./template-editor-tab.component.scss']
})
export class TemplateEditorTabComponent {
  readonly store = inject(WhatsAppStore);

  readonly messageTypes: { key: WhatsAppMessageType; label: string }[] = [
    { key: 'AttendanceCheckIn', label: 'تأكيد الحضور' },
    { key: 'AbsenceAlert', label: 'تنبيه الغياب' },
    { key: 'ExamResult', label: 'نتيجة الاختبار' },
    { key: 'WeeklyParentReport', label: 'التقرير الأسبوعي' },
    { key: 'PaymentReceipt', label: 'إيصال السداد' },
    { key: 'AttendanceCorrection', label: 'تصحيح الحضور' }
  ];

  templateText = signal<string>('');
  isSaving = signal<boolean>(false);
  saveSuccess = signal<boolean>(false);

  readonly activeType = computed(() => this.store.activeTemplateType());

  readonly currentTemplate = computed(() => {
    return this.store.currentTemplate();
  });

  readonly previewText = computed(() => {
    let text = this.templateText() || this.currentTemplate()?.rawText || '';
    text = text.replace(/{StudentName}/g, 'أحمد محمود علي');
    text = text.replace(/{SessionTime}/g, '04:30 مساءً');
    text = text.replace(/{CenterName}/g, 'سنتر الأوائل');
    text = text.replace(/{ExamTitle}/g, 'اختبار النحو والبلاغة');
    text = text.replace(/{Score}/g, '19');
    text = text.replace(/{MaxScore}/g, '20');
    text = text.replace(/{Percentage}/g, '95%');
    text = text.replace(/{Amount}/g, '450.00');
    text = text.replace(/{ReceiptNumber}/g, 'REC-2026-0042');
    text = text.replace(/{Date}/g, '2026/10/21');
    return text;
  });

  selectType(type: WhatsAppMessageType): void {
    this.store.activeTemplateType.set(type);
    const tmpl = this.store.templates()[type];
    this.templateText.set(tmpl ? tmpl.rawText : '');
    this.saveSuccess.set(false);
  }

  insertToken(token: string): void {
    const current = this.templateText() || this.currentTemplate()?.rawText || '';
    this.templateText.set(current + ` {${token}} `);
  }

  async saveTemplate(): Promise<void> {
    const type = this.activeType();
    const text = this.templateText() || this.currentTemplate()?.rawText || '';
    this.isSaving.set(true);
    this.saveSuccess.set(false);
    try {
      await this.store.updateTemplate(type, text);
      this.saveSuccess.set(true);
      setTimeout(() => this.saveSuccess.set(false), 3000);
    } finally {
      this.isSaving.set(false);
    }
  }
}
