import { Component, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ACADEMIC_VALIDATION_REGEX } from '../../../../../core/models/academic.models';

@Component({
  selector: 'app-link-global-student-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './link-global-student-modal.component.html',
  styleUrls: ['./link-global-student-modal.component.scss']
})
export class LinkGlobalStudentModalComponent {
  readonly linked = output<{ studentCode: string; secretPin: string }>();
  readonly closed = output<void>();

  studentCode = '';
  secretPin = '';
  validationError = signal<string | null>(null);
  isSubmitting = signal(false);

  get isFormValid(): boolean {
    const codeValid = this.studentCode.trim().length >= 4;
    const pinValid = ACADEMIC_VALIDATION_REGEX.SECRET_PIN.test(this.secretPin.trim());
    return codeValid && pinValid;
  }

  submit(): void {
    const code = this.studentCode.trim();
    const pin = this.secretPin.trim();

    if (!code) {
      this.validationError.set('يرجى إدخال كود الطالب الموحد');
      return;
    }

    if (!ACADEMIC_VALIDATION_REGEX.SECRET_PIN.test(pin)) {
      this.validationError.set('الرقم السري للربط يجب أن يتكون من 4 أرقام بالضبط (DEC-02)');
      return;
    }

    this.validationError.set(null);
    this.isSubmitting.set(true);
    this.linked.emit({ studentCode: code, secretPin: pin });
  }

  close(): void {
    this.closed.emit();
  }
}
