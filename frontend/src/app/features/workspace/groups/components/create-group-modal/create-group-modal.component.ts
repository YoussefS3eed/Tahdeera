import { Component, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StudyGroupItem } from '../../../../../core/models/academic.models';

@Component({
  selector: 'app-create-group-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-group-modal.component.html',
  styleUrls: ['./create-group-modal.component.scss']
})
export class CreateGroupModalComponent {
  readonly created = output<Partial<StudyGroupItem>>();
  readonly closed = output<void>();

  name = '';
  grade = 'Grade1Secondary';
  subject = 'الفيزياء';
  defaultMonthlyFee = 350;
  lateGraceMinutes = 15;
  maxCapacity = 30;
  teacherInChargeName = 'أ. حسام الدين';

  validationError = signal<string | null>(null);

  get isValid(): boolean {
    return this.name.trim().length >= 3 && !!this.grade && this.maxCapacity > 0;
  }

  submit(): void {
    if (!this.isValid) {
      this.validationError.set('يرجى إدخال اسم المجموعة (3 أحرف على الأقل) وتحديد المرحلة الدراسية والسعة');
      return;
    }

    this.validationError.set(null);
    this.created.emit({
      name: this.name.trim(),
      grade: this.grade as any,
      subject: this.subject.trim(),
      defaultMonthlyFee: this.defaultMonthlyFee,
      lateGraceMinutes: this.lateGraceMinutes,
      maxCapacity: this.maxCapacity,
      teacherInChargeName: this.teacherInChargeName.trim(),
      currentEnrollmentCount: 0,
      weeklyScheduleSummary: 'جلسة أسبوعية'
    });
  }

  close(): void {
    this.closed.emit();
  }
}
