import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SettingsStore } from '../../../../../core/stores/settings.store';

@Component({
  selector: 'app-attendance-policies-card',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './attendance-policies-card.component.html',
  styleUrls: ['./attendance-policies-card.component.scss']
})
export class AttendancePoliciesCardComponent {
  readonly store = inject(SettingsStore);

  lateThresholdMinutes = signal<number>(15);
  autoAbsentThresholdMinutes = signal<number>(45);
  autoSendAbsenceWhatsApp = signal<boolean>(true);
  autoSendCheckInWhatsApp = signal<boolean>(true);

  saveSuccess = signal<boolean>(false);

  ngOnInit(): void {
    const s = this.store.settings();
    const p = s?.attendancePolicies;
    if (p) {
      this.lateThresholdMinutes.set(p.lateThresholdMinutes);
      this.autoAbsentThresholdMinutes.set(p.autoAbsentThresholdMinutes);
      this.autoSendAbsenceWhatsApp.set(p.autoSendAbsenceWhatsApp);
      this.autoSendCheckInWhatsApp.set(p.autoSendCheckInWhatsApp);
    } else if (s) {
      this.lateThresholdMinutes.set(s.defaultLateGraceMinutes || 15);
    }
  }

  async savePolicies(): Promise<void> {
    await this.store.updateAttendancePolicies({
      lateThresholdMinutes: this.lateThresholdMinutes(),
      autoAbsentThresholdMinutes: this.autoAbsentThresholdMinutes(),
      autoSendAbsenceWhatsApp: this.autoSendAbsenceWhatsApp(),
      autoSendCheckInWhatsApp: this.autoSendCheckInWhatsApp()
    });
    this.saveSuccess.set(true);
    setTimeout(() => this.saveSuccess.set(false), 3000);
  }
}
