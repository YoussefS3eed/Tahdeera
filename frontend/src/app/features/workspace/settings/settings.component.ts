import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SettingsStore } from '../../../core/stores/settings.store';
import { CenterProfileCardComponent } from './components/center-profile-card/center-profile-card.component';
import { AttendancePoliciesCardComponent } from './components/attendance-policies-card/attendance-policies-card.component';
import { SubscriptionViewCardComponent } from './components/subscription-view-card/subscription-view-card.component';
import { DataBackupCardComponent } from './components/data-backup-card/data-backup-card.component';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule,
    CenterProfileCardComponent,
    AttendancePoliciesCardComponent,
    SubscriptionViewCardComponent,
    DataBackupCardComponent
  ],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {
  readonly store = inject(SettingsStore);

  async ngOnInit(): Promise<void> {
    await Promise.all([
      this.store.loadSettings(),
      this.store.loadSubscription()
    ]);
  }
}
