import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WhatsAppStore } from '../../../../../core/stores/whatsapp.store';
import { WhatsAppMessageLog } from '../../../../../core/models/whatsapp.models';

@Component({
  selector: 'app-outbox-logs-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './outbox-logs-tab.component.html',
  styleUrls: ['./outbox-logs-tab.component.scss']
})
export class OutboxLogsTabComponent {
  readonly store = inject(WhatsAppStore);

  cancelMessage(id: string): void {
    this.store.cancelPendingMessage(id);
  }

  retryMessage(id: string): void {
    this.store.retryFailedMessage(id);
  }
}
