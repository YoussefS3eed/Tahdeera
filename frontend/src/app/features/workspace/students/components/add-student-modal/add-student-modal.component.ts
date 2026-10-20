import { Component, input, output, signal, computed, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StudyGroupItem, ACADEMIC_VALIDATION_REGEX } from '../../../../../core/models/academic.models';
import { NfcWedgeAdapterService } from '../../../../../core/services/nfc-wedge-adapter.service';

@Component({
  selector: 'app-add-student-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-student-modal.component.html',
  styleUrls: ['./add-student-modal.component.scss']
})
export class AddStudentModalComponent implements OnInit {
  private readonly nfcWedge = inject(NfcWedgeAdapterService);

  readonly availableGroups = input<StudyGroupItem[]>([]);

  readonly created = output<{
    fullName: string;
    grade: string;
    studentCode: string;
    guardianName: string;
    guardianPhone: string;
    whatsappOptIn: boolean;
    groupIds: string[];
    initialNfcCardUid?: string;
    defaultMonthlyFee?: number;
  }>();

  readonly closed = output<void>();

  currentStep = signal<1 | 2 | 3>(1);

  // Step 1: Academic Identity
  fullName = '';
  grade = 'Grade1Secondary';
  studentCode = '';

  // Step 2: Guardian & WhatsApp
  guardianName = '';
  guardianPhone = '';
  whatsappOptIn = true;

  // Step 3: Groups & NFC Card
  selectedGroupId = '';
  initialNfcCardUid = '';
  defaultMonthlyFee = 350;

  validationError: string | null = null;

  ngOnInit(): void {
    // Generate default code ST-XXXX
    this.studentCode = 'ST-' + Math.floor(1000 + Math.random() * 9000);

    // Listen to NFC tap on Step 3
    this.nfcWedge.uidScanned$.subscribe(uid => {
      if (this.currentStep() === 3) {
        this.initialNfcCardUid = uid;
      }
    });
  }

  // Filter groups matching student's grade (CONF-05)
  gradeGroups = computed(() => {
    return this.availableGroups().filter(g => g.grade === this.grade);
  });

  get isStep1Valid(): boolean {
    return this.fullName.trim().length >= 3 && !!this.grade && !!this.studentCode.trim();
  }

  get isStep2Valid(): boolean {
    const phoneValid = ACADEMIC_VALIDATION_REGEX.EGYPTIAN_PHONE.test(this.guardianPhone.trim());
    return this.guardianName.trim().length >= 3 && phoneValid;
  }

  get isStep3Valid(): boolean {
    return true; // Groups & NFC are optional or default selectable
  }

  nextStep(): void {
    this.validationError = null;
    if (this.currentStep() === 1) {
      if (!this.isStep1Valid) {
        this.validationError = 'يرجى إدخال اسم الطالب ثلاثياً (3 أحرف على الأقل) واختيار المرحلة الدراسية';
        return;
      }
      this.currentStep.set(2);
    } else if (this.currentStep() === 2) {
      if (!this.isStep2Valid) {
        this.validationError = 'رقم هاتف ولي الأمر غير صالح. يجب أن يكون رقم مصري صالح (مثال: 01019482687)';
        return;
      }
      this.currentStep.set(3);
    }
  }

  prevStep(): void {
    this.validationError = null;
    if (this.currentStep() === 3) this.currentStep.set(2);
    else if (this.currentStep() === 2) this.currentStep.set(1);
  }

  submit(): void {
    this.created.emit({
      fullName: this.fullName.trim(),
      grade: this.grade,
      studentCode: this.studentCode.trim(),
      guardianName: this.guardianName.trim(),
      guardianPhone: this.guardianPhone.trim(),
      whatsappOptIn: this.whatsappOptIn,
      groupIds: this.selectedGroupId ? [this.selectedGroupId] : [],
      initialNfcCardUid: this.initialNfcCardUid.trim() || undefined,
      defaultMonthlyFee: this.defaultMonthlyFee
    });
  }

  close(): void {
    this.closed.emit();
  }
}
