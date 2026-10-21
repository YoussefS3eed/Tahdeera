import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LmsStore } from '../../../../../core/stores/lms.store';
import { ExamItem } from '../../../../../core/models/lms.models';

@Component({
  selector: 'app-create-exam-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-exam-modal.component.html',
  styleUrls: ['./create-exam-modal.component.scss']
})
export class CreateExamModalComponent {
  private lmsStore = inject(LmsStore);

  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() examCreated = new EventEmitter<ExamItem>();

  title = signal<string>('');
  subject = signal<string>('لغة عربية');
  durationMinutes = signal<number>(45);
  totalPoints = signal<number>(20);
  maxAttempts = signal<number>(1);
  autoSendWhatsapp = signal<boolean>(true);
  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  async submit(): Promise<void> {
    if (!this.title().trim()) {
      this.errorMessage.set('يرجى إدخال عنوان الاختبار');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    try {
      const res = await this.lmsStore.createExam({
        title: this.title().trim(),
        subject: this.subject(),
        durationMinutes: this.durationMinutes(),
        totalPoints: this.totalPoints(),
        maxAttempts: this.maxAttempts(),
        status: 'Draft',
        autoSendWhatsappResults: this.autoSendWhatsapp(),
        questionsCount: 0,
        submissionCount: 0
      });
      this.examCreated.emit(res);
      this.closeModal();
    } catch (err: any) {
      this.errorMessage.set(err.message || 'حدث خطأ أثناء إنشاء الاختبار');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  closeModal(): void {
    this.title.set('');
    this.errorMessage.set(null);
    this.close.emit();
  }
}
