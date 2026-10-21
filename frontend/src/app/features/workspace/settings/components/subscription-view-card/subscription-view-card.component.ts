import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SettingsStore } from '../../../../../core/stores/settings.store';

@Component({
  selector: 'app-subscription-view-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './subscription-view-card.component.html',
  styleUrls: ['./subscription-view-card.component.scss']
})
export class SubscriptionViewCardComponent {
  readonly store = inject(SettingsStore);
}
