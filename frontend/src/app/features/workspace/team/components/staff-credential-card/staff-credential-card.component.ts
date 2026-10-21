import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WorkspaceStaffMember } from '../../../../../core/models/team-settings.models';

@Component({
  selector: 'app-staff-credential-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './staff-credential-card.component.html',
  styleUrls: ['./staff-credential-card.component.scss']
})
export class StaffCredentialCardComponent {
  @Input() member: WorkspaceStaffMember | null = null;
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();

  copied = signal<boolean>(false);

  copyWhatsAppCard(): void {
    if (!this.member) return;

    const message = `مرحباً بك أستاذ/ة *${this.member.displayName || this.member.name}* في منصة تحضيرة (سنتر الأوائل التعليمي).
يسعدنا انضمامك لفريق العمل بدور: *${this.member.role}*.

📌 بيانات تسجيل الدخول الخاصة بك:
- رابط المنصة: https://app.tahdeera.eg/login
- اسم المستخدم: \`${this.member.username}\`
- كلمة المرور: \`${this.member.initialPasswordCleartext || '••••••••'}\`

يرجى الاحتفاظ بهذه البيانات بسرية تامة.`;

    navigator.clipboard.writeText(message).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 3000);
    });
  }

  closeModal(): void {
    this.close.emit();
  }
}
