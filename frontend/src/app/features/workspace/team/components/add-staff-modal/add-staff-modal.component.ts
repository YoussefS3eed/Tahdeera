import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TeamStore } from '../../../../../core/stores/team.store';
import { WorkspaceStaffRole, WorkspaceStaffMember } from '../../../../../core/models/team-settings.models';

@Component({
  selector: 'app-add-staff-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-staff-modal.component.html',
  styleUrls: ['./add-staff-modal.component.scss']
})
export class AddStaffModalComponent {
  readonly teamStore = inject(TeamStore);

  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() staffCreated = new EventEmitter<WorkspaceStaffMember>();

  displayName = signal<string>('');
  username = signal<string>('');
  email = signal<string>('');
  phone = signal<string>('');
  role = signal<WorkspaceStaffRole>('Assistant');
  password = signal<string>('');

  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  generateStrongPassword(): void {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%';
    let pwd = '';
    for (let i = 0; i < 10; i++) {
      pwd += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    this.password.set(pwd);
  }

  async submit(): Promise<void> {
    const dName = this.displayName().trim();
    const uName = this.username().trim();
    const em = this.email().trim();
    const ph = this.phone().trim();
    const pwd = this.password().trim();

    if (!dName || !uName || !em || !ph || !pwd) {
      this.errorMessage.set('يرجى ملء جميع الحقول المطلوبة');
      return;
    }

    // Validate Egyptian Phone
    const phoneRegex = /^01[0125][0-9]{8}$/;
    if (!phoneRegex.test(ph)) {
      this.errorMessage.set('يرجى إدخال رقم هاتف مصري صحيح يبدأ بـ 010 أو 011 أو 012 أو 015');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    try {
      const res = await this.teamStore.addMember({
        displayName: dName,
        username: uName,
        email: em,
        phone: ph,
        role: this.role(),
        password: pwd
      });

      this.staffCreated.emit(res);
      this.closeModal();
    } catch (err: any) {
      this.errorMessage.set(err.message || 'حدث خطأ أثناء إضافة الموظف');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  closeModal(): void {
    this.displayName.set('');
    this.username.set('');
    this.email.set('');
    this.phone.set('');
    this.password.set('');
    this.errorMessage.set(null);
    this.close.emit();
  }
}
