import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TeamStore } from '../../../../../core/stores/team.store';
import { WorkspaceStaffMember } from '../../../../../core/models/team-settings.models';

@Component({
  selector: 'app-deactivate-staff-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './deactivate-staff-modal.component.html',
  styleUrls: ['./deactivate-staff-modal.component.scss']
})
export class DeactivateStaffModalComponent {
  private teamStore = inject(TeamStore);

  @Input() member: WorkspaceStaffMember | null = null;
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() deactivated = new EventEmitter<WorkspaceStaffMember>();

  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  async confirmDeactivation(): Promise<void> {
    if (!this.member) return;
    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    try {
      const res = await this.teamStore.deactivateMember(this.member.id);
      this.deactivated.emit(res);
      this.closeModal();
    } catch (err: any) {
      this.errorMessage.set(err.message || 'حدث خطأ أثناء تعطيل حساب الموظف');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  closeModal(): void {
    this.errorMessage.set(null);
    this.close.emit();
  }
}
